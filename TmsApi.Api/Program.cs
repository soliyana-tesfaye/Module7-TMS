using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
//using Scalar.Aspire;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

using TmsApi.Api.ExceptionHandlers;
using TmsApi.Api.Filters;
using TmsApi.Api.Middleware;
using TmsApi.Api.RateLimiting;

using TmsApi.Application.Behaviors;
using TmsApi.Application.Features.Enrollments.Commands;
using TmsApi.Application.Features.Enrollments.Handlers;
using TmsApi.Application.Interfaces;

using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;


var builder = WebApplication.CreateBuilder(args);



#region CQRS / MediatR / Validation


builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(EnrollStudentHandler).Assembly));


builder.Services.AddValidatorsFromAssembly(
    typeof(EnrollStudentValidator).Assembly);



builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(LoggingBehavior<,>));


builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));



builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails();



#endregion




#region Cache


builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions =
        new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(10),

            LocalCacheExpiration =
                TimeSpan.FromMinutes(2)
        };
});


#endregion





#region Rate Limiting


builder.Services.AddRateLimiter(options =>
{

    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext =>
            {

                var (partitionKey, tier) =
                    ApiKeyResolver.Resolve(httpContext);


                return tier switch
                {

                    ApiKeyTier.Paid =>
                        RateLimitPartition.GetTokenBucketLimiter(
                            $"paid:{partitionKey}",
                            _ => new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 200,

                                TokensPerPeriod = 100,

                                ReplenishmentPeriod =
                                    TimeSpan.FromSeconds(10),

                                QueueLimit = 0,

                                AutoReplenishment = true
                            }),



                    ApiKeyTier.Free =>
                        RateLimitPartition.GetTokenBucketLimiter(
                            $"free:{partitionKey}",
                            _ => new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 30,

                                TokensPerPeriod = 10,

                                ReplenishmentPeriod =
                                    TimeSpan.FromSeconds(10),

                                QueueLimit = 0,

                                AutoReplenishment = true
                            }),



                    _ =>
                        RateLimitPartition.GetTokenBucketLimiter(
                            $"anon:{partitionKey}",
                            _ => new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 10,

                                TokensPerPeriod = 5,

                                ReplenishmentPeriod =
                                    TimeSpan.FromSeconds(10),

                                QueueLimit = 0,

                                AutoReplenishment = true
                            })
                };

            });



    options.AddConcurrencyLimiter(
        "transcripts",
        limiter =>
        {

            limiter.PermitLimit = 5;

            limiter.QueueLimit = 20;

            limiter.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;

        });



    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;



    options.OnRejected =
        async (context, ct) =>
        {

            var retryAfter = "10";


            if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retry))
            {

                retryAfter =
                    ((int)retry.TotalSeconds).ToString();

            }



            context.HttpContext.Response.Headers.RetryAfter =
                retryAfter;



            context.HttpContext.Response.ContentType =
                "application/problem+json";



            await context.HttpContext.Response
                .WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Title = "Rate limit exceeded",

                        Detail =
                            $"Too many requests. Retry after {retryAfter} seconds.",

                        Status =
                            StatusCodes.Status429TooManyRequests,

                        Type =
                            "https://tms.local/errors/rate_limit_exceeded"
                    },
                    ct);

        };

});



#endregion






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
    options.ShouldInclude =
        description =>
            description.GroupName == "v1";
});



builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude =
        description =>
            description.GroupName == "v2";
});





builder.Services.AddApiVersioning(options =>
{

    options.DefaultApiVersion =
        new ApiVersion(1, 0);


    options.AssumeDefaultVersionWhenUnspecified =
        true;


    options.ReportApiVersions =
        true;



    options.ApiVersionReader =
        ApiVersionReader.Combine(

            new UrlSegmentApiVersionReader(),

            new HeaderApiVersionReader(
                "X-Api-Version")

        );

})
.AddApiExplorer(options =>
{

    options.GroupNameFormat =
        "'v'VVV";


    options.SubstituteApiVersionInUrl =
        true;

});





builder.Services.AddDbContext<TmsDbContext>(
    options =>
    {

        options.UseNpgsql(
            builder.Configuration
                .GetConnectionString("TmsDatabase"));


        options.LogTo(Console.WriteLine,
            LogLevel.Information);


        options.EnableSensitiveDataLogging();

    });





builder.Services.AddScoped<ICourseService, CourseService>();

builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();



builder.Services.AddAuthorization();



#endregion





var app = builder.Build();






#region Middleware


app.UseExceptionHandler();



app.UseMiddleware<RequestLoggingMiddleware>();



if (app.Environment.IsDevelopment())
{

    app.MapOpenApi("/openapi/{documentName}.json");


    app.MapScalarApiReference(options =>
    {

        options
            .WithTitle("TMS API Reference")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(
                ScalarTarget.CSharp,
                ScalarClient.HttpClient)
            .AddDocument("v1",
                "API Version 1.0")
            .AddDocument("v2",
                "API Version 2.0");

    });

}





app.UseAuthorization();



app.UseRateLimiter();



app.UseMiddleware<V1DeprecationMiddleware>();



#endregion






#region Development Seeder


if (app.Environment.IsDevelopment())
{

    using var scope =
        app.Services.CreateScope();



    var context =
        scope.ServiceProvider
            .GetRequiredService<TmsDbContext>();


    await DataSeeder.SeedAsync(context);

}


#endregion





app.MapControllers();



app.Run();