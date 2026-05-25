using AutoMapper;
using MediatR;
using CourseService.Application.CQRS.Queries;
using CourseService.Application.DTOs;
using CourseService.Domain.Interfaces;
using Common.Exceptions;

namespace CourseService.Application.CQRS.Handlers;

/// <summary>
/// Handler for getting all courses with pagination.
/// </summary>
public class GetAllCoursesQueryHandler : IRequestHandler<GetAllCoursesQuery, PaginatedDto<CourseDto>>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IMapper _mapper;

    public GetAllCoursesQueryHandler(ICourseRepository courseRepository, IMapper mapper)
    {
        _courseRepository = courseRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedDto<CourseDto>> Handle(GetAllCoursesQuery request, CancellationToken cancellationToken)
    {
        var (courses, totalCount) = await _courseRepository.GetCoursesWithPaginationAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var courseDtos = _mapper.Map<IEnumerable<CourseDto>>(courses);

        return new PaginatedDto<CourseDto>
        {
            Data = courseDtos,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Handler for getting a course by ID.
/// </summary>
public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, CourseDto>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IMapper _mapper;

    public GetCourseByIdQueryHandler(ICourseRepository courseRepository, IMapper mapper)
    {
        _courseRepository = courseRepository;
        _mapper = mapper;
    }

    public async Task<CourseDto> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null)
        {
            throw new NotFoundException("Course", request.CourseId);
        }

        return _mapper.Map<CourseDto>(course);
    }
}

/// <summary>
/// Handler for searching courses.
/// </summary>
public class SearchCoursesQueryHandler : IRequestHandler<SearchCoursesQuery, IEnumerable<CourseDto>>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IMapper _mapper;

    public SearchCoursesQueryHandler(ICourseRepository courseRepository, IMapper mapper)
    {
        _courseRepository = courseRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CourseDto>> Handle(SearchCoursesQuery request, CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.SearchCoursesAsync(
            request.StartDate,
            request.EndDate,
            request.InstructorName,
            cancellationToken);

        return _mapper.Map<IEnumerable<CourseDto>>(courses);
    }
}

/// <summary>
/// Handler for getting enrolled courses for a student.
/// </summary>
public class GetEnrolledCoursesQueryHandler : IRequestHandler<GetEnrolledCoursesQuery, IEnumerable<CourseDto>>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IMapper _mapper;

    public GetEnrolledCoursesQueryHandler(ICourseRepository courseRepository, IMapper mapper)
    {
        _courseRepository = courseRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CourseDto>> Handle(GetEnrolledCoursesQuery request, CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.GetEnrolledCoursesAsync(request.StudentId, cancellationToken);
        return _mapper.Map<IEnumerable<CourseDto>>(courses);
    }
}

/// <summary>
/// Handler for getting courses by instructor.
/// </summary>
public class GetCoursesByInstructorQueryHandler : IRequestHandler<GetCoursesByInstructorQuery, IEnumerable<CourseDto>>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IMapper _mapper;

    public GetCoursesByInstructorQueryHandler(ICourseRepository courseRepository, IMapper mapper)
    {
        _courseRepository = courseRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CourseDto>> Handle(GetCoursesByInstructorQuery request, CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.GetCoursesByInstructorAsync(request.InstructorId, cancellationToken);
        return _mapper.Map<IEnumerable<CourseDto>>(courses);
    }
}
