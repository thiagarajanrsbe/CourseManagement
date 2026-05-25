namespace CourseService.Domain.Entities;

/// <summary>
/// Course domain entity.
/// </summary>
public class Course
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid InstructorId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    private Course() { }

    /// <summary>
    /// Factory method to create a new Course.
    /// </summary>
    public static Course Create(string title, string description, Guid instructorId, DateTime startDate, DateTime endDate)
    {
        ValidateDates(startDate, endDate);

        return new Course
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            InstructorId = instructorId,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow,
            Enrollments = new List<Enrollment>()
        };
    }

    /// <summary>
    /// Updates course information.
    /// </summary>
    public void Update(string title, string description, DateTime startDate, DateTime endDate)
    {
        ValidateDates(startDate, endDate);

        Title = title;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    /// Validates that end date is after start date.
    /// </summary>
    private static void ValidateDates(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
        {
            throw new ArgumentException("End date must be after start date.");
        }
    }
}

/// <summary>
/// Enrollment domain entity representing a student's enrollment in a course.
/// </summary>
public class Enrollment
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public DateTime EnrolledAt { get; set; }
    public Course? Course { get; set; }

    private Enrollment() { }

    /// <summary>
    /// Factory method to create a new Enrollment.
    /// </summary>
    public static Enrollment Create(Guid studentId, Guid courseId)
    {
        return new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = courseId,
            EnrolledAt = DateTime.UtcNow
        };
    }
}
