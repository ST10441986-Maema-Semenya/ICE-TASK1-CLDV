namespace UniversityEnrollment.Models;

// ----- Request/response DTOs for the API -----

public record CourseDto(
    string Department,
    string CourseCode,
    string CourseName,
    string InstructorId,
    string InstructorName,
    string Description,
    int Capacity,
    int EnrolledCount);

public record CreateCourseRequest(
    string Department,
    string CourseCode,
    string CourseName,
    string InstructorId,
    string InstructorName,
    string Description,
    int Capacity);

public record UpdateCourseRequest(
    string? CourseName,
    string? InstructorId,
    string? InstructorName,
    string? Description,
    int? Capacity);

public record StudentDto(
    string StudentId,
    string Name,
    string Email,
    List<string> EnrolledCourses);

public record CreateStudentRequest(
    string StudentId,
    string Name,
    string Email);

public record UpdateStudentRequest(
    string? Name,
    string? Email);

public record EnrollRequest(
    string StudentId,
    string Department,
    string CourseCode);

/// <summary>
/// The message contract placed on the "CourseEnrollmentQueue". Keeping this as
/// its own type (rather than reusing EnrollRequest) means the wire format of the
/// queue is decoupled from the shape of the public API.
/// </summary>
public record EnrollmentQueueMessage(
    string StudentId,
    string Department,
    string CourseCode,
    DateTimeOffset RequestedAtUtc);
