using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Dtos;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Caching;

namespace TmsApi.Infrastructure.Services;

public class CachedCourseService(
    HybridCache cache,
    ICourseService courseService,
    ILogger<CachedCourseService> logger)
    : ICachedCourseService
{
    public async Task<CourseSummaryDto> GetCourseAsync(
        string code,
        CancellationToken ct)
    {
        var key = CacheKeys.Course(code);
        var dbHit = false;

        var dto = await cache.GetOrCreateAsync(
            key,
            code,
            async (state, token) =>
            {
                dbHit = true;

                logger.LogInformation(
                    "Cache MISS for {Key}, fetching from DB",
                    key);

                var course = await courseService
                    .GetByCodeAsync(state, token);

                if (course is null)
                {
                    throw new KeyNotFoundException(
                        $"Course {state} not found.");
                }

                return new CourseSummaryDto
                {
                    Id = course.Id,
                    Code = course.Code,
                    Title = course.Title,
                    MaxCapacity = course.MaxCapacity,
                    EnrollmentCount = course.Enrollments.Count
                };
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation(
                "Cache HIT for {Key}",
                key);
        }

        return dto;
    }


    public async Task<List<CourseSummaryDto>> GetAllCoursesAsync(
        CancellationToken ct)
    {
        var key = CacheKeys.CoursesAll;
        var dbHit = false;

        var list = await cache.GetOrCreateAsync(
            key,
            courseService,
            async (state, token) =>
            {
                dbHit = true;

                logger.LogInformation(
                    "Cache MISS for {Key}, fetching from DB",
                    key);

                var result = await state.GetCoursesAsync(
                    new PagedRequest
                    {
                        Page = 1,
                        PageSize = 50
                    },
                    token);

                return result.Items
                    .Select(c => new CourseSummaryDto
                    {
                        Id = c.Id,
                        Code = c.Code,
                        Title = c.Title,
                        MaxCapacity = c.MaxCapacity,
                        EnrollmentCount = c.EnrollmentCount
                    })
                    .ToList();
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation(
                "Cache HIT for {Key}",
                key);
        }

        return list;
    }


    public async Task InvalidateCourseCacheAsync(
        CancellationToken ct)
    {
        logger.LogInformation(
            "Invalidating cache tag {Tag}",
            CacheKeys.CoursesTag);

        await cache.RemoveByTagAsync(
            CacheKeys.CoursesTag,
            ct);
    }
}
