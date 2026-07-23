using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Domain.Entities;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;


public class CourseService : ICourseService
{
    private readonly TmsDbContext _context;
    private readonly ILogger<CourseService> _logger;

    public CourseService(
        TmsDbContext context,
        ILogger<CourseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            MaxCapacity = request.MaxCapacity
        };

        _context.Courses.Add(course);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created course {CourseId}", course.Id);

        return (await GetByIdAsync(course.Id, ct))!;
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct)
    {
        return _context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Code == code, ct);
    }

    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(
    PagedRequest request,
    CancellationToken ct)
{
    IQueryable<Course> query = _context.Courses.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        query = query.Where(c =>
            EF.Functions.ILike(c.Title, $"%{request.Search}%") ||
            EF.Functions.ILike(c.Code, $"%{request.Search}%"));
    }

    var totalCount = await query.CountAsync(ct);

    query = request.OrderBy switch
    {
        "Code" => request.Descending
            ? query.OrderByDescending(c => c.Code)
            : query.OrderBy(c => c.Code),

        "MaxCapacity" => request.Descending
            ? query.OrderByDescending(c => c.MaxCapacity)
            : query.OrderBy(c => c.MaxCapacity),

        _ => request.Descending
            ? query.OrderByDescending(c => c.Title)
            : query.OrderBy(c => c.Title)
    };

    var items = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(c => new CourseResponseDto(
            c.Id,
            c.Code,
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count))
        .ToListAsync(ct);

    return new PagedResponse<CourseResponseDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}
}