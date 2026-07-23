using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly TmsDbContext _context;

    public ReportsController(TmsDbContext context)
    {
        _context = context;
    }

    [HttpGet("top-courses")]
    public async Task<IActionResult> GetTopCourses()
    {
        var result = await _context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                Title = g.Key,
                EnrollmentCount = g.Count()
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .Take(5)
            .ToListAsync();

        return Ok(result);
    }
}