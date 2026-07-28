using MediatR;
using TmsApi.Application.Common;

namespace TmsApi.Application.Features.Enrollments.Commands;

public record EnrollStudentCommand(
    int StudentId,
    string CourseCode
) : IRequest<Result<EnrollmentCreated, EnrollmentError>>;


public record EnrollmentCreated(
    int EnrollmentId,
    int StudentId,
    string CourseCode);