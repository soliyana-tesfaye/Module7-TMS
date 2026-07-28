using TmsApi.Application.Dtos;

namespace TmsApi.Application.Interfaces;

public interface ICachedCourseService
{
    Task<CourseSummaryDto> GetCourseAsync(
        string code,
        CancellationToken ct);

    Task<List<CourseSummaryDto>> GetAllCoursesAsync(
        CancellationToken ct);

    Task InvalidateCourseCacheAsync(
        CancellationToken ct);
}