using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MediatR;
using Serilog;
using CourseService.Api.Middleware;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Application.Mapping;
using CourseService.Application.Validators;
using CourseService.Domain.Interfaces;
using CourseService.Infrastructure.Persistence;
using CourseService.Infrastructure.Repositories;
using CourseService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSettings["Secret"]!;
var jwtIssuer = jwtSettings["Issuer"]!;
var jwtAudience = jwtSettings["Audience"]!;

// EF Core with In-Memory database
builder.Services.AddDbContext<CourseServiceDbContext>(options =>
{
    options.UseInMemoryDatabase("CourseServiceDb");
});

// AutoMapper
var courseMapperConfig = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<CourseMappingProfile>());
var courseMapper = courseMapperConfig.CreateMapper();
builder.Services.AddSingleton(courseMapper);

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateCourseDtoValidator));
builder.Services.AddFluentValidationAutoValidation();

// Repositories
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

// Forward incoming bearer token when calling UserService
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CourseService.Api.Handlers.AuthorizationHeaderHandler>();

// HTTP Client for inter-service communication
var userServiceUrl = builder.Configuration["Services:UserServiceUrl"]!;
builder.Services.AddHttpClient<IUserService, UserService>(client =>
{
    client.BaseAddress = new Uri(userServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddHttpMessageHandler<CourseService.Api.Handlers.AuthorizationHeaderHandler>()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = builder.Environment.IsDevelopment()
        ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        : null
});

// MediatR
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,
        typeof(CourseService.Application.CQRS.Handlers.CreateCourseCommandHandler).Assembly);
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InstructorPolicy", policy =>
        policy.RequireRole("Instructor"));

    options.AddPolicy("StudentPolicy", policy =>
        policy.RequireRole("Student"));

    options.AddPolicy("AuthenticatedPolicy", policy =>
        policy.RequireAuthenticatedUser());
});

// Swagger/OpenAPI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Course Service API",
        Version = "v1",
        Description = "Course management service with CQRS pattern"
    });

    var xmlFile = "CourseService.Api.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    // JWT Bearer scheme
    options.AddSecurityDefinition("Bearer", new()
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

// Controllers
builder.Services.AddControllers();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CourseServiceDbContext>();
    dbContext.Database.EnsureCreated();

    // Seed initial courses if empty
    if (!dbContext.Courses.Any())
    {
        var instructor1Id = new Guid("11111111-1111-1111-1111-111111111111");
        var student1Id = new Guid("22222222-2222-2222-2222-222222222222");

        var course1 = CourseService.Domain.Entities.Course.Create(
            "Course One",
            "Course covers advanced .net topics.",
            instructor1Id,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(60));
        course1.Id = Guid.NewGuid();

        var course2 = CourseService.Domain.Entities.Course.Create(
            "Course 2 - Entity Framework Core",
            "data access with Entity Framework Core.",
            instructor1Id,
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow.AddDays(50));
        course2.Id = Guid.NewGuid();

        dbContext.Courses.Add(course1);
        dbContext.Courses.Add(course2);
        dbContext.SaveChanges();

        Log.Information("Initial data inserted with sample courses.");
    }
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Course Service API v1");
    });
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("Starting Course Service...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
