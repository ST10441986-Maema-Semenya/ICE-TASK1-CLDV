using Azure;
using Azure.Data.Tables;

namespace UniversityEnrollment.Models;

/// <summary>
/// Represents a course row in the "Courses" Azure Table.
/// PartitionKey groups courses by department (e.g. "CS", "MATH").
/// RowKey is the unique course code (e.g. "CS101").
/// </summary>
public class CourseModel : ITableEntity
{
    // ITableEntity required members
    public string PartitionKey { get; set; } = string.Empty; // Department code
    public string RowKey { get; set; } = string.Empty;        // Course code, e.g. "CS101"
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Domain properties
    public string CourseName { get; set; } = string.Empty;
    public string InstructorId { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int EnrolledCount { get; set; }

    public CourseModel() { }

    public CourseModel(string department, string courseCode)
    {
        PartitionKey = department;
        RowKey = courseCode;
    }
}
