using System.ComponentModel.DataAnnotations;

namespace UniversityEnrollment.Services;

public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    [Required(AllowEmptyStrings = false, ErrorMessage =
        "AzureStorage:TableStorageConnectionString is missing from configuration. " +
        "Check appsettings.json (or user-secrets/environment variables) for the 'AzureStorage' section.")]
    public string TableStorageConnectionString { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage =
        "AzureStorage:QueueStorageConnectionString is missing from configuration. " +
        "Check appsettings.json (or user-secrets/environment variables) for the 'AzureStorage' section.")]
    public string QueueStorageConnectionString { get; set; } = string.Empty;

    public string CoursesTableName { get; set; } = "Courses";
    public string StudentsTableName { get; set; } = "Students";
    public string EnrollmentQueueName { get; set; } = "CourseEnrollmentQueue";
}
