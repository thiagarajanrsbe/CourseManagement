using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CourseService.Application.CQRS.Commands;
using CourseService.Application.CQRS.Queries;
using CourseService.Application.DTOs;
using Common.Exceptions;

namespace CourseService.Api.Controllers;

/// <summary>
/// Courses controller for course management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<CreateCourseDto> _createValidator;
    private readonly IValidator<UpdateCourseDto> _updateValidator;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(
        IMediator mediator,
        IValidator<CreateCourseDto> createValidator,
        IValidator<UpdateCourseDto> updateValidator,
        ILogger<CoursesController> logger)
    {
        _mediator = mediator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get all courses with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedDto<CourseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedDto<CourseDto>>>> GetCourses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching courses - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

        var query = new GetAllCoursesQuery { PageNumber = pageNumber, PageSize = pageSize };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<PaginatedDto<CourseDto>>.SuccessResponse(result, "Courses retrieved successfully."));
    }

    /// <summary>
    /// Get course by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseDto>>> GetCourseById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching course with ID: {CourseId}", id);

        var query = new GetCourseByIdQuery { CourseId = id };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<CourseDto>.SuccessResponse(result, "Course retrieved successfully."));
    }

    /// <summary>
    /// Search courses by date range and instructor name.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CourseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<CourseDto>>>> SearchCourses(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? instructorName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching courses - StartDate: {StartDate}, EndDate: {EndDate}, Instructor: {InstructorName}",
            startDate, endDate, instructorName);

        var query = new SearchCoursesQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            InstructorName = instructorName
        };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<CourseDto>>.SuccessResponse(result, "Courses found."));
    }

    /// <summary>
    /// Create a new course (Instructor only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Instructor")]
    [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseDto>>> CreateCourse(
        [FromBody] CreateCourseDto dto,
        CancellationToken cancellationToken = default)
    {
        // Validate request
        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            _logger.LogWarning("Course creation validation failed.");
            return BadRequest(ApiResponse<CourseDto>.ErrorResponse("Validation failed.", errors));
        }

        var instructorId = GetUserIdFromToken();

        _logger.LogInformation("Creating course by instructor: {InstructorId}", instructorId);

        var command = new CreateCourseCommand
        {
            Title = dto.Title,
            Description = dto.Description,
            InstructorId = instructorId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };

        var result = await _mediator.Send(command, cancellationToken);
        _logger.LogInformation("Course created successfully: {CourseId}", result.Id);

        return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, 
            ApiResponse<CourseDto>.SuccessResponse(result, "Course created successfully."));
    }

    /// <summary>
    /// Update a course (Instructor only).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Instructor")]
    [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CourseDto>>> UpdateCourse(
        Guid id,
        [FromBody] UpdateCourseDto dto,
        CancellationToken cancellationToken = default)
    {
        // Validate request
        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            _logger.LogWarning("Course update validation failed for course: {CourseId}", id);
            return BadRequest(ApiResponse<CourseDto>.ErrorResponse("Validation failed.", errors));
        }

        var instructorId = GetUserIdFromToken();

        _logger.LogInformation("Updating course {CourseId} by instructor {InstructorId}", id, instructorId);

        var command = new UpdateCourseCommand
        {
            CourseId = id,
            InstructorId = instructorId,
            Title = dto.Title,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };

        var result = await _mediator.Send(command, cancellationToken);
        _logger.LogInformation("Course updated successfully: {CourseId}", result.Id);

        return Ok(ApiResponse<CourseDto>.SuccessResponse(result, "Course updated successfully."));
    }

    /// <summary>
    /// Delete a course (Instructor only).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Instructor")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> DeleteCourse(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var instructorId = GetUserIdFromToken();

        _logger.LogInformation("Deleting course {CourseId} by instructor {InstructorId}", id, instructorId);

        var command = new DeleteCourseCommand
        {
            CourseId = id,
            InstructorId = instructorId
        };

        await _mediator.Send(command, cancellationToken);
        _logger.LogInformation("Course deleted successfully: {CourseId}", id);

        return Ok(ApiResponse.SuccessResponse("Course deleted successfully."));
    }

    /// <summary>
    /// Enroll student in a course.
    /// </summary>
    [HttpPost("{courseId}/enroll/{studentId}")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EnrollmentDto>>> EnrollStudent(
        Guid courseId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var requestingUserId = GetUserIdFromToken();

        _logger.LogInformation("Enrollment request - Course: {CourseId}, Student: {StudentId}, RequestingUser: {RequestingUserId}",
            courseId, studentId, requestingUserId);

        var command = new EnrollStudentCommand
        {
            CourseId = courseId,
            StudentId = studentId,
            RequestingUserId = requestingUserId
        };

        var result = await _mediator.Send(command, cancellationToken);
        _logger.LogInformation("Student enrolled successfully - Enrollment: {EnrollmentId}", result.Id);

        return CreatedAtAction(nameof(GetCourseById), new { id = courseId },
            ApiResponse<EnrollmentDto>.SuccessResponse(result, "Student enrolled successfully."));
    }

    /// <summary>
    /// Get enrolled courses for current student.
    /// </summary>
    [HttpGet("enrolled")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CourseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<CourseDto>>>> GetEnrolledCourses(
        CancellationToken cancellationToken = default)
    {
        var studentId = GetUserIdFromToken();

        _logger.LogInformation("Fetching enrolled courses for student: {StudentId}", studentId);

        var query = new GetEnrolledCoursesQuery { StudentId = studentId };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<CourseDto>>.SuccessResponse(result, "Enrolled courses retrieved successfully."));
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new Common.Exceptions.UnauthorizedAccessException("Invalid user token.");
        }
        return userId;
    }
}
