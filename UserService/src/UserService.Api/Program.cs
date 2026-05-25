using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using UserService.Api.Middleware;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.Mapping;
using UserService.Application.Validators;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;

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
var jwtExpirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

// EF Core with In-Memory database
builder.Services.AddDbContext<UserServiceDbContext>(options =>
{
    options.UseInMemoryDatabase("UserServiceDb");
});

// AutoMapper
var mapperConfig = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile<UserMappingProfile>());
var mapper = mapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining(typeof(RegisterUserDtoValidator));
builder.Services.AddFluentValidationAutoValidation();

// Repositories and Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthenticationService>(provider =>
{
    var userRepository = provider.GetRequiredService<IUserRepository>();
    return new AuthenticationService(userRepository, jwtSecret, jwtIssuer, jwtAudience, jwtExpirationMinutes);
});
builder.Services.AddScoped<IUserService, UserApplicationService>();

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

// Swagger/OpenAPI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "User Service API",
        Version = "v1",
        Description = "User management and authentication service"
    });

    var xmlFile = "UserService.Api.xml";
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
                Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
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

// Initialize database with data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UserServiceDbContext>();
    dbContext.Database.EnsureCreated();
    
    // Insert initial data if empty
    if (!dbContext.Users.Any())
    {
        var instructorPassword = "Instructor@123";
        var studentPassword = "Student@123";

        var instructorHash = HashPassword(instructorPassword);
        var studentHash = HashPassword(studentPassword);

        var instructor = UserService.Domain.Entities.User.Create(
            "instructor",
            "instructor@course.com",
            instructorHash,
            UserService.Domain.Entities.UserRole.Instructor);
        instructor.Id = new Guid("11111111-1111-1111-1111-111111111111");
        instructor.IsActive = true;

        var student = UserService.Domain.Entities.User.Create(
            "student",
            "student@course.com",
            studentHash,
            UserService.Domain.Entities.UserRole.Student);
        student.Id = new Guid("22222222-2222-2222-2222-222222222222");
        student.IsActive = true;

        dbContext.Users.Add(instructor);
        dbContext.Users.Add(student);
        dbContext.SaveChanges();

        Log.Information("Sample users are inserted in table.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
    });
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("Starting User Service...");

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

// Helper function for password hashing
static string HashPassword(string password)
{
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hashedBytes);
}
