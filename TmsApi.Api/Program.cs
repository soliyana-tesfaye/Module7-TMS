using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Api.Filters;
using TmsApi.Infrastructure.Services;
using TmsApi.Application.Interfaces;
using TmsApi.Api.Middleware;
var builder = WebApplication.CreateBuilder(args);

#region Services

builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
        ReferenceHandler.IgnoreCycles;
});

builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description =>
        description.GroupName == "v1";
});

builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description =>
        description.GroupName == "v2";
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddProblemDetails();

builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("TmsDatabase"))
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging());

builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

builder.Services.AddAuthorization();

#endregion

var app = builder.Build();

#region Middleware

app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.MapOpenApi("/openapi/{documentName}.json");
   app.MapScalarApiReference(options =>
{
    options
        .WithTitle("TMS API Reference")
        .WithTheme(ScalarTheme.DeepSpace)
        .WithDefaultHttpClient(
            ScalarTarget.CSharp,
            ScalarClient.HttpClient)
        .AddDocument("v1", "API Version 1.0")
        .AddDocument("v2", "API Version 2.0");
});
}

//app.UseHttpsRedirection();

// app.UseAuthentication();

app.UseAuthorization();

#endregion



app.UseMiddleware<V1DeprecationMiddleware>();
#region Development Data Seeder

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider
        .GetRequiredService<TmsDbContext>();

    await DataSeeder.SeedAsync(context);
}
app.MapControllers();
#endregion

app.Run();