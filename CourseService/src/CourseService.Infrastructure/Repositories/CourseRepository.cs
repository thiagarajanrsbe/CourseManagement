using Microsoft.EntityFrameworkCore;
using CourseService.Domain.Entities;
using CourseService.Domain.Interfaces;
using CourseService.Infrastructure.Persistence;

namespace CourseService.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly CourseServiceDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(CourseServiceDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        await Task.CompletedTask;
    }

    public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Course repository implementation.
/// </summary>
public class CourseRepository : Repository<Course>, ICourseRepository
{
    public CourseRepository(CourseServiceDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Course>> GetCoursesByInstructorAsync(Guid instructorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.InstructorId == instructorId)
            .Include(c => c.Enrollments)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Course>> SearchCoursesAsync(DateTime? startDate, DateTime? endDate, string? instructorName, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(c => c.Enrollments).AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(c => c.StartDate >= startDate);
        }

        if (endDate.HasValue)
        {
            query = query.Where(c => c.EndDate <= endDate);
        }

        // Note: In real scenario, this would need to call UserService to filter by instructor name
        // For now, we're leaving this for future enhancement

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Course> Courses, int TotalCount)> GetCoursesWithPaginationAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await _dbSet.CountAsync(cancellationToken);

        var courses = await _dbSet
            .Include(c => c.Enrollments)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (courses, totalCount);
    }

    public async Task<IEnumerable<Course>> GetEnrolledCoursesAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.Enrollments.Any(e => e.StudentId == studentId))
            .Include(c => c.Enrollments)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Enrollment repository implementation.
/// </summary>
public class EnrollmentRepository : Repository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(CourseServiceDbContext context) : base(context)
    {
    }

    public async Task<Enrollment?> GetEnrollmentAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(
            e => e.StudentId == studentId && e.CourseId == courseId,
            cancellationToken);
    }

    public async Task<bool> IsEnrolledAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(
            e => e.StudentId == studentId && e.CourseId == courseId,
            cancellationToken);
    }

    public async Task<IEnumerable<Enrollment>> GetEnrollmentsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.CourseId == courseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Enrollment>> GetEnrollmentsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.StudentId == studentId)
            .ToListAsync(cancellationToken);
    }
}
