using MediatR;
using CourseService.Application.DTOs;

namespace CourseService.Application.CQRS.Commands;

/// <summary>
/// Command to create a course.
/// </summary>
public class CreateCourseCommand : IRequest<CourseDto>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid InstructorId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Command to update a course.
/// </summary>
public class UpdateCourseCommand : IRequest<CourseDto>
{
    public Guid CourseId { get; set; }
    public Guid InstructorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Command to delete a course.
/// </summary>
public class DeleteCourseCommand : IRequest<Unit>
{
    public Guid CourseId { get; set; }
    public Guid InstructorId { get; set; }
}

/// <summary>
/// Command to enroll a student in a course.
/// </summary>
public class EnrollStudentCommand : IRequest<EnrollmentDto>
{
    public Guid CourseId { get; set; }
    public Guid StudentId { get; set; }
    public Guid RequestingUserId { get; set; }
}
