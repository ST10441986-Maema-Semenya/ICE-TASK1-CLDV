using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;
using UniversityEnrollment.Models;

namespace UniversityEnrollment.Services;

/// <summary>
/// (h) Background worker that continuously polls "CourseEnrollmentQueue" and,
/// for every enrolment request message, updates the enrolled-courses list for
/// the corresponding student (and increments the course's enrolled count).
/// Runs for the lifetime of the application as an ASP.NET Core hosted service.
/// </summary>
public class EnrollmentQueueProcessor : BackgroundService
{
    private readonly QueueClient _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnrollmentQueueProcessor> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public EnrollmentQueueProcessor(
        IOptions<AzureStorageOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<EnrollmentQueueProcessor> logger)
    {
        var settings = options.Value;
        var queueServiceClient = new QueueServiceClient(settings.QueueStorageConnectionString);
        _queue = queueServiceClient.GetQueueClient(settings.EnrollmentQueueName.ToLowerInvariant());
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _queue.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Pull up to 10 messages at a time; make them invisible for 30s while we process them.
                QueueMessage[] messages = await _queue.ReceiveMessagesAsync(
                    maxMessages: 10,
                    visibilityTimeout: TimeSpan.FromSeconds(30),
                    cancellationToken: stoppingToken);

                if (messages.Length == 0)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                    continue;
                }

                foreach (var message in messages)
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Shutting down.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while polling {QueueName}", _queue.Name);
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(QueueMessage message, CancellationToken stoppingToken)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<EnrollmentQueueMessage>(message.MessageText);
            if (payload is null)
            {
                _logger.LogWarning("Could not deserialize message {MessageId}; deleting.", message.MessageId);
                await _queue.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                return;
            }

            // Each message handling gets its own DI scope since the table services
            // are registered as scoped/singleton-safe wrappers around TableClient.
            using var scope = _scopeFactory.CreateScope();
            var courseService = scope.ServiceProvider.GetRequiredService<CourseService>();
            var studentService = scope.ServiceProvider.GetRequiredService<StudentService>();

            var course = await courseService.GetCourseAsync(payload.Department, payload.CourseCode);
            var student = await studentService.GetStudentAsync(payload.StudentId);

            if (course is null || student is null)
            {
                _logger.LogWarning(
                    "Skipping enrolment for student {StudentId} in course {Department}:{CourseCode} - not found.",
                    payload.StudentId, payload.Department, payload.CourseCode);
                await _queue.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                return;
            }

            var incremented = await courseService.TryIncrementEnrollmentAsync(payload.Department, payload.CourseCode);
            if (!incremented)
            {
                _logger.LogWarning(
                    "Course {Department}:{CourseCode} is full; enrolment for {StudentId} rejected.",
                    payload.Department, payload.CourseCode, payload.StudentId);
                await _queue.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                return;
            }

            await studentService.AddCourseToStudentAsync(payload.StudentId, payload.Department, payload.CourseCode);

            _logger.LogInformation(
                "Enrolled student {StudentId} in course {Department}:{CourseCode}.",
                payload.StudentId, payload.Department, payload.CourseCode);

            // Remove the message only after the update succeeds so a crash mid-processing
            // leaves the message to be retried once its visibility timeout expires.
            await _queue.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId}; it will retry after its visibility timeout.", message.MessageId);
            // Do not delete - let it become visible again and be retried (or dead-lettered
            // manually after DequeueCount exceeds a threshold, which you can add here).
        }
    }
}
