using CourseService.Domain.Entities;

namespace CourseService.Domain.Interfaces;

/// <summary>
/// Generic repository interface for data access operations.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Course repository interface.
/// </summary>
public interface ICourseRepository : IRepository<Course>
{
    Task<IEnumerable<Course>> GetCoursesByInstructorAsync(Guid instructorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Course>> SearchCoursesAsync(DateTime? startDate, DateTime? endDate, string? instructorName, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Course> Courses, int TotalCount)> GetCoursesWithPaginationAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Course>> GetEnrolledCoursesAsync(Guid studentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Enrollment repository interface.
/// </summary>
public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<Enrollment?> GetEnrollmentAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
    Task<bool> IsEnrolledAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Enrollment>> GetEnrollmentsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Enrollment>> GetEnrollmentsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
}
