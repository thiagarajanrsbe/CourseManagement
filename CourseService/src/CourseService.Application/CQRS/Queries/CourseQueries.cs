using MediatR;
using CourseService.Application.DTOs;

namespace CourseService.Application.CQRS.Queries;

/// <summary>
/// Query to get all courses with pagination.
/// </summary>
public class GetAllCoursesQuery : IRequest<PaginatedDto<CourseDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Query to get a course by ID.
/// </summary>
public class GetCourseByIdQuery : IRequest<CourseDto>
{
    public Guid CourseId { get; set; }
}

/// <summary>
/// Query to search courses.
/// </summary>
public class SearchCoursesQuery : IRequest<IEnumerable<CourseDto>>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? InstructorName { get; set; }
}

/// <summary>
/// Query to get enrolled courses for a student.
/// </summary>
public class GetEnrolledCoursesQuery : IRequest<IEnumerable<CourseDto>>
{
    public Guid StudentId { get; set; }
}

/// <summary>
/// Query to get courses by instructor.
/// </summary>
public class GetCoursesByInstructorQuery : IRequest<IEnumerable<CourseDto>>
{
    public Guid InstructorId { get; set; }
}
