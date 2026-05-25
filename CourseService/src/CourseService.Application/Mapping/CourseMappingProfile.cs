using AutoMapper;
using CourseService.Application.DTOs;
using CourseService.Domain.Entities;

namespace CourseService.Application.Mapping;

/// <summary>
/// AutoMapper profile for Course and Enrollment entities and DTOs.
/// </summary>
public class CourseMappingProfile : Profile
{
    public CourseMappingProfile()
    {
        CreateMap<Course, CourseDto>()
            .ForMember(dest => dest.EnrollmentCount, opt => opt.MapFrom(src => src.Enrollments.Count));

        CreateMap<CreateCourseDto, Course>();
        CreateMap<UpdateCourseDto, Course>();
        CreateMap<Enrollment, EnrollmentDto>();
    }
}
