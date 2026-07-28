using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[Route("api/courses")]
[ApiVersion("2.0")]
[ApiExplorerSettings(GroupName = "v2")]

public class CoursesController(
    ICachedCourseService cachedCourseService) : ControllerBase
{
    [HttpGet("{code}")]
    public async Task<IActionResult> GetCourse(
        string code,
        CancellationToken ct)
    {
        var course = await cachedCourseService
            .GetCourseAsync(code, ct);

        return Ok(course);
    }


    [HttpGet]
    public async Task<IActionResult> GetCourses(
        CancellationToken ct)
    {
        var courses = await cachedCourseService
            .GetAllCoursesAsync(ct);

        return Ok(courses);
    }
}