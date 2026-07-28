using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Domain.Entities;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly TmsDbContext _context;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(
        TmsDbContext context,
        ILogger<EnrollmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<EnrollmentResponseDto?> GetByIdAsync(
        int courseId,
        int id,
        CancellationToken ct)
    {
        return _context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt))
            .FirstOrDefaultAsync(ct);
    }
public async Task<bool> ExistsAsync(
    int studentId,
    string courseCode,
    CancellationToken ct)
{
    return await _context.Enrollments
        .AnyAsync(
            e =>
                e.StudentId == studentId &&
                e.Course.Code == courseCode,
            ct);
}

public async Task AddAsync(
    Enrollment enrollment,
    CancellationToken ct)
{
    _context.Enrollments.Add(enrollment);

    await _context.SaveChangesAsync(ct);
}

public async Task<List<Enrollment>> GetByStudentIdAsync(
    int studentId,
    CancellationToken ct)
{
    return await _context.Enrollments
        .Include(e => e.Course)
        .Where(e => e.StudentId == studentId)
        .ToListAsync(ct);
}

    public async Task<EnrollmentResponseDto> CreateAsync(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct)
    {
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        _context.Enrollments.Add(enrollment);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created enrollment {EnrollmentId}",
            enrollment.Id);

        return (await GetByIdAsync(courseId, enrollment.Id, ct))!;
    }

    public Task<List<EnrollmentResponseDto>> GetByCourseAsync(
    int courseId,
    CancellationToken ct)
{
    return _context.Enrollments
        .AsNoTracking()
        .Where(e => e.CourseId == courseId)
        .Select(e => new EnrollmentResponseDto(
            e.Id,
            e.CourseId,
            e.StudentId,
            e.EnrolledAt))
        .ToListAsync(ct);
}
}