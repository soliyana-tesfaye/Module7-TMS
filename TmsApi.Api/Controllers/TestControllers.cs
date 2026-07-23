using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly TmsDbContext context;

    public TestController(TmsDbContext context)
    {
        this.context = context;
    }

    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        var results = context.Students
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
            .AsNoTracking()
            .Where(s => s.GPA >= 3.0m)
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.RegistrationNumber,
                s.Name,
                s.GPA,
                s.IsActive,
                Enrollments = s.Enrollments.Select(e => new
                {
                    e.CourseId,
                    Course = e.Course.Title,
                    e.Grade
                })
            })
            .ToList();

        return Ok(results);
    }
}