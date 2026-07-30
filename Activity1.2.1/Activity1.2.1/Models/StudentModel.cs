using Azure;
using Azure.Data.Tables;

namespace UniversityEnrollment.Models;

/// <summary>
/// Represents a student row in the "Students" Azure Table.
/// PartitionKey is a fixed value ("Student") since student lookups are by RowKey
/// (StudentId), and the total student count is not large enough to need extra
/// partitioning. RowKey is the unique student ID.
///
/// Azure Table Storage does not support list/array properties natively, so the
/// list of enrolled course keys is stored as a delimited string
/// ("CS:CS101;MATH:MATH201") and exposed to the rest of the app as a List&lt;string&gt;
/// through the helper methods below.
/// </summary>
public class StudentModel : ITableEntity
{
    public string PartitionKey { get; set; } = "Student";
    public string RowKey { get; set; } = string.Empty; // StudentId
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Serialized as "PartitionKey:RowKey;PartitionKey:RowKey;..."
    public string EnrolledCourseKeys { get; set; } = string.Empty;

    public StudentModel() { }

    public StudentModel(string studentId)
    {
        RowKey = studentId;
    }

    public List<(string CoursePartitionKey, string CourseRowKey)> GetEnrolledCourses()
    {
        if (string.IsNullOrWhiteSpace(EnrolledCourseKeys))
            return new();

        return EnrolledCourseKeys
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(entry =>
            {
                var parts = entry.Split(':', 2);
                return (parts[0], parts.Length > 1 ? parts[1] : string.Empty);
            })
            .ToList();
    }

    public void AddEnrolledCourse(string coursePartitionKey, string courseRowKey)
    {
        var existing = GetEnrolledCourses();
        if (existing.Any(c => c.CoursePartitionKey == coursePartitionKey && c.CourseRowKey == courseRowKey))
            return; // already enrolled

        existing.Add((coursePartitionKey, courseRowKey));
        EnrolledCourseKeys = string.Join(';', existing.Select(c => $"{c.CoursePartitionKey}:{c.CourseRowKey}"));
    }

    public void RemoveEnrolledCourse(string coursePartitionKey, string courseRowKey)
    {
        var existing = GetEnrolledCourses()
            .Where(c => !(c.CoursePartitionKey == coursePartitionKey && c.CourseRowKey == courseRowKey))
            .ToList();

        EnrolledCourseKeys = string.Join(';', existing.Select(c => $"{c.CoursePartitionKey}:{c.CourseRowKey}"));
    }
}
