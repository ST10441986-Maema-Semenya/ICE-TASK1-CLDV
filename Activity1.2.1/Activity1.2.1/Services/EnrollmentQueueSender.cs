using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using UniversityEnrollment.Models;

namespace UniversityEnrollment.Services;

/// <summary>
/// (e) Establishes the connection to the Azure Queue storage account and
/// (g) sends a message to "CourseEnrollmentQueue" whenever a student enrols
/// in a course. The controller that handles enrolment requests calls
/// EnqueueEnrollmentAsync instead of writing directly to the Students table,
/// so the actual roster update happens asynchronously via the queue processor.
/// </summary>
public class EnrollmentQueueSender
{
    private readonly QueueClient _queue;

    public EnrollmentQueueSender(IOptions<AzureStorageOptions> options)
    {
        var settings = options.Value;

        var queueServiceClient = new QueueServiceClient(settings.QueueStorageConnectionString);
        _queue = queueServiceClient.GetQueueClient(settings.EnrollmentQueueName.ToLowerInvariant());

        // Creates the "CourseEnrollmentQueue" queue if it doesn't already exist.
        _queue.CreateIfNotExists();
    }

    public async Task EnqueueEnrollmentAsync(string studentId, string department, string courseCode)
    {
        var message = new EnrollmentQueueMessage(studentId, department, courseCode, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(message);

        // Azure Queue messages must be UTF-8/Base64 safe text; the SDK handles
        // Base64 encoding for us automatically when using SendMessageAsync(string).
        await _queue.SendMessageAsync(json);
    }
}
