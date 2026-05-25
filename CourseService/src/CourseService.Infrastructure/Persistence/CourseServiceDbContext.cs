using Microsoft.EntityFrameworkCore;
using CourseService.Domain.Entities;

namespace CourseService.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for CourseService.
/// </summary>
public class CourseServiceDbContext : DbContext
{
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;

    public CourseServiceDbContext(DbContextOptions<CourseServiceDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Course entity
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.InstructorId)
                .IsRequired();

            entity.Property(e => e.StartDate)
                .IsRequired();

            entity.Property(e => e.EndDate)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // One-to-many: Course has many Enrollments
            entity.HasMany(e => e.Enrollments)
                .WithOne(e => e.Course)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.InstructorId);
        });

        // Configure Enrollment entity
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.StudentId)
                .IsRequired();

            entity.Property(e => e.CourseId)
                .IsRequired();

            entity.Property(e => e.EnrolledAt)
                .IsRequired();

            // Unique constraint: A student can only enroll once in a course
            entity.HasIndex(e => new { e.StudentId, e.CourseId })
                .IsUnique();

            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.CourseId);
        });
    }
}
