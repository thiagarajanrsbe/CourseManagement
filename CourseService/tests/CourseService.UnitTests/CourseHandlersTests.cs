using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Xunit;
using CourseService.Application.CQRS.Handlers;
using CourseService.Application.CQRS.Commands;
using CourseService.Application.CQRS.Queries;
using CourseService.Application.DTOs;
using CourseService.Application.Mapping;
using CourseService.Domain.Entities;
using CourseService.Domain.Interfaces;
using CourseService.Application.Interfaces;
using Common.Exceptions;

namespace CourseService.UnitTests;

public class CourseHandlersTests
{
    private readonly IMapper _mapper;

    public CourseHandlersTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile(new CourseMappingProfile()));
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task CreateCourse_Success()
    {
        var courseRepo = new Mock<ICourseRepository>();
        courseRepo.Setup(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        courseRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var userSvc = new Mock<IUserService>();
        userSvc.Setup(u => u.UserExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new CreateCourseCommandHandler(courseRepo.Object, _mapper, userSvc.Object);

        var cmd = new CreateCourseCommand
        {
            Title = "Test Course",
            Description = "Desc",
            InstructorId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10)
        };

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.Equal(cmd.Title, result.Title);
        courseRepo.Verify(r => r.AddAsync(It.IsAny<Course>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCourse_InstructorNotFound_Throws()
    {
        var courseRepo = new Mock<ICourseRepository>();
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(u => u.UserExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new CreateCourseCommandHandler(courseRepo.Object, _mapper, userSvc.Object);

        var cmd = new CreateCourseCommand { InstructorId = Guid.NewGuid(), Title = "x", Description = "y", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow };

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task GetCourseById_Success()
    {
        var id = Guid.NewGuid();
        var course = Course.Create("C", "D", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        course.Id = id;

        var courseRepo = new Mock<ICourseRepository>();
        courseRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(course);

        var handler = new GetCourseByIdQueryHandler(courseRepo.Object, _mapper);

        var result = await handler.Handle(new GetCourseByIdQuery { CourseId = id }, CancellationToken.None);

        Assert.Equal(course.Title, result.Title);
    }

    [Fact]
    public async Task GetCourseById_NotFound_Throws()
    {
        var courseRepo = new Mock<ICourseRepository>();
        courseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Course?)null);

        var handler = new GetCourseByIdQueryHandler(courseRepo.Object, _mapper);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetCourseByIdQuery { CourseId = Guid.NewGuid() }, CancellationToken.None));
    }

    [Fact]
    public async Task EnrollStudent_Success()
    {
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var course = Course.Create("C", "D", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        course.Id = courseId;

        var courseRepo = new Mock<ICourseRepository>();
        courseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>())).ReturnsAsync(course);

        var enrollmentRepo = new Mock<IEnrollmentRepository>();
        enrollmentRepo.Setup(r => r.IsEnrolledAsync(studentId, courseId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        enrollmentRepo.Setup(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        enrollmentRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var userSvc = new Mock<IUserService>();
        userSvc.Setup(u => u.UserExistsAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new EnrollStudentCommandHandler(courseRepo.Object, enrollmentRepo.Object, _mapper, userSvc.Object);

        var cmd = new EnrollStudentCommand { CourseId = courseId, StudentId = studentId, RequestingUserId = studentId };

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.Equal(studentId, result.StudentId);
    }    

    [Fact]
    public async Task EnrollStudent_Unauthorized_Throws()
    {
        var courseId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var courseRepo = new Mock<ICourseRepository>();
        var enrollmentRepo = new Mock<IEnrollmentRepository>();
        var userSvc = new Mock<IUserService>();

        var handler = new EnrollStudentCommandHandler(courseRepo.Object, enrollmentRepo.Object, _mapper, userSvc.Object);

        var cmd = new EnrollStudentCommand { CourseId = courseId, StudentId = studentId, RequestingUserId = Guid.NewGuid() };

        await Assert.ThrowsAsync<System.UnauthorizedAccessException>(() => handler.Handle(cmd, CancellationToken.None));
    }
}
