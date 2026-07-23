using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.Dtos;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController : ControllerBase
{
    private readonly TmsDbContext _context;

    public StudentsController(TmsDbContext context)
    {
        _context = context;
    }

    // =========================
    // GET ALL STUDENTS
    // =========================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
    {
        var students = await _context.Students
            .AsNoTracking()
            .ToListAsync();

        return Ok(students);
    }

    // =========================
    // PAGINATION
    // =========================
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<StudentDto>>> GetPagedStudents(
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _context.Students
            .AsNoTracking()
            .OrderBy(s => s.Name);

        var totalCount = await query.CountAsync();

        var students = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                RegistrationNumber = s.RegistrationNumber,
                Name = s.Name,
                GPA = s.GPA,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(new PagedResult<StudentDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Data = students
        });
    }

    // =========================
    // TOP COURSES
    // =========================
    [HttpGet("top-courses")]
    public async Task<ActionResult> GetTopCourses()
    {
        var result = await _context.Enrollments
            .Include(e => e.Course)
            .GroupBy(e => e.Course.Title)
            .Select(g => new CourseSummaryDto
            {
                Title = g.Key,
                EnrollmentCount = g.Count()
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .Take(5)
            .ToListAsync();

        return Ok(result);
    }

    // =========================
    // STUDENT REPORT (N+1 FIXED)
    // =========================
    [HttpGet("student-report")]
    public async Task<ActionResult> GetStudentReport()
    {
        var report = await _context.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count
            })
            .ToListAsync();

       // return Ok(report);
       var students = await _context.Students
    .AsNoTracking()
    .Select(s => new StudentDto
    {
        Id = s.Id,
        RegistrationNumber = s.RegistrationNumber,
        Name = s.Name,
        GPA = s.GPA,
        IsActive = s.IsActive,

        Version = EF.Property<uint>(s, "xmin")
    })
    .ToListAsync();

return Ok(students);
    }

    // =========================
    // CREATE STUDENT
    // =========================
    [HttpPost]
    public async Task<ActionResult> CreateStudent([FromBody] StudentDto dto)
    {
        if (dto.RegistrationNumber.Length > 20)
            return BadRequest("RegistrationNumber must be max 20 characters");

        if (dto.Name.Length > 100)
            return BadRequest("Name must be max 100 characters");

        var student = new Student
        {
            RegistrationNumber = dto.RegistrationNumber,
            Name = dto.Name,
            GPA = dto.GPA,
            IsActive = dto.IsActive
        };

        _context.Students.Add(student);

        // Shadow property
        _context.Entry(student)
            .Property("LastUpdated")
            .CurrentValue = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(student);
    }

    // =========================
    // UPDATE STUDENT
    // =========================
    [HttpPut("{id}")]
public async Task<ActionResult> UpdateStudent(int id, StudentDto dto)
{
    var student = await _context.Students.FindAsync(id);

    if (student == null)
        return NotFound();

    student.RegistrationNumber = dto.RegistrationNumber;
    student.Name = dto.Name;
    student.GPA = dto.GPA;
    student.IsActive = dto.IsActive;

    _context.Entry(student)
        .Property("LastUpdated")
        .CurrentValue = DateTime.UtcNow;

    try
    {
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        return Conflict("This record was modified by another user.");
    }

    return Ok(student);
}


[HttpGet("admin")]
public async Task<ActionResult<IEnumerable<Student>>> GetAllStudentsAdmin()
{
    var students = await _context.Students
        .IgnoreQueryFilters()
        .AsNoTracking()
        .ToListAsync();

    return Ok(students);
}

[HttpPost("archive-enrollments")]
public async Task<ActionResult> ArchiveEnrollments()
{
    var cutoff = DateTime.UtcNow.AddYears(-1);

    var rows = await _context.Enrollments
        .Where(e => e.EnrolledAt < cutoff)
        .ExecuteUpdateAsync(s =>
            s.SetProperty(e => e.IsArchived, true));

    return Ok($"{rows} enrollments archived.");
}
}