using AutoMapper;
using MediatR;
using CourseService.Application.CQRS.Commands;
using CourseService.Application.DTOs;
using CourseService.Domain.Entities;
using CourseService.Domain.Interfaces;
using Common.Exceptions;
using CourseService.Application.Interfaces;
using UnauthorizedAccessException = Common.Exceptions.UnauthorizedAccessException;

namespace CourseService.Application.CQRS.Handlers;

/// <summary>
/// Handler for creating a course.
/// </summary>
public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CourseDto>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IMapper _mapper;
    private readonly IUserService _userService;

    public CreateCourseCommandHandler(ICourseRepository courseRepository, IMapper mapper, IUserService userService)
    {
        _courseRepository = courseRepository;
        _mapper = mapper;
        _userService = userService;
    }

    public async Task<CourseDto> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        // Verify instructor exists
        var instructorExists = await _userService.UserExistsAsync(request.InstructorId);
        if (!instructorExists)
        {
            throw new NotFoundException("Instructor", request.InstructorId);
        }

        var course = Course.Create(
            request.Title,
            request.Description,
            request.InstructorId,
            request.StartDate,
            request.EndDate);

        await _courseRepository.AddAsync(course, cancellationToken);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CourseDto>(course);
    }
}

/// <summary>
/// Handler for updating a course.
/// </summary>
public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, CourseDto>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IMapper _mapper;

    public UpdateCourseCommandHandler(ICourseRepository courseRepository, IMapper mapper)
    {
        _courseRepository = courseRepository;
        _mapper = mapper;
    }

    public async Task<CourseDto> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null)
        {
            throw new NotFoundException("Course", request.CourseId);
        }

        // Only the instructor can update their course
        if (course.InstructorId != request.InstructorId)
        {
            throw new Common.Exceptions.UnauthorizedAccessException("Only the course instructor can update this course.");
        }

        course.Update(
            request.Title,
            request.Description,
            request.StartDate,
            request.EndDate);

        await _courseRepository.UpdateAsync(course, cancellationToken);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CourseDto>(course);
    }
}

/// <summary>
/// Handler for deleting a course.
/// </summary>
public class DeleteCourseCommandHandler : IRequestHandler<DeleteCourseCommand, Unit>
{
    private readonly ICourseRepository _courseRepository;

    public DeleteCourseCommandHandler(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<Unit> Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null)
        {
            throw new NotFoundException("Course", request.CourseId);
        }

        // Only the instructor can delete their course
        if (course.InstructorId != request.InstructorId)
        {
            throw new UnauthorizedAccessException("Only the course instructor can delete this course.");
        }

        await _courseRepository.DeleteAsync(course, cancellationToken);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

/// <summary>
/// Handler for enrolling a student in a course.
/// </summary>
public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, EnrollmentDto>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IMapper _mapper;
    private readonly IUserService _userService;

    public EnrollStudentCommandHandler(
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        IMapper mapper,
        IUserService userService)
    {
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
        _mapper = mapper;
        _userService = userService;
    }

    public async Task<EnrollmentDto> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        // Students can only enroll themselves
        if (request.StudentId != request.RequestingUserId)
        {
            throw new System.UnauthorizedAccessException("Students can only enroll themselves.");
        }

        // Verify course exists
        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null)
        {
            throw new NotFoundException("Course", request.CourseId);
        }

        // Verify student exists
        var studentExists = await _userService.UserExistsAsync(request.StudentId);
        if (!studentExists)
        {
            throw new NotFoundException("Student", request.StudentId);
        }

        // Check if student is already enrolled
        var alreadyEnrolled = await _enrollmentRepository.IsEnrolledAsync(request.StudentId, request.CourseId, cancellationToken);
        if (alreadyEnrolled)
        {
            throw new DuplicateException("Student is already enrolled in this course.");
        }

        var enrollment = Enrollment.Create(request.StudentId, request.CourseId);

        await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
        await _enrollmentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<EnrollmentDto>(enrollment);
    }
}
