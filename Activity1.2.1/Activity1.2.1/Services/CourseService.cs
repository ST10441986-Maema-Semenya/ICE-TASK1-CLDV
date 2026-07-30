using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using UniversityEnrollment.Models;

namespace UniversityEnrollment.Services;

/// <summary>
/// Handles the connection to the "Courses" table and all CRUD operations
/// against it. This is item (e) + (f) of the spec for the Courses table.
/// </summary>
public class CourseService
{
    private readonly TableClient _table;

    public CourseService(IOptions<AzureStorageOptions> options)
    {
        var settings = options.Value;

        // (e) Establish the connection to the Azure Table storage account.
        var serviceClient = new TableServiceClient(settings.TableStorageConnectionString);
        _table = serviceClient.GetTableClient(settings.CoursesTableName);

        // Creates the "Courses" table if it doesn't already exist (safe to call every startup).
        _table.CreateIfNotExists();
    }

    // ----- Create -----
    public async Task<CourseModel> AddCourseAsync(CreateCourseRequest request)
    {
        var entity = new CourseModel(request.Department, request.CourseCode)
        {
            CourseName = request.CourseName,
            InstructorId = request.InstructorId,
            InstructorName = request.InstructorName,
            Description = request.Description,
            Capacity = request.Capacity,
            EnrolledCount = 0
        };

        await _table.AddEntityAsync(entity);
        return entity;
    }

    // ----- Read (single) -----
    public async Task<CourseModel?> GetCourseAsync(string department, string courseCode)
    {
        try
        {
            var response = await _table.GetEntityAsync<CourseModel>(department, courseCode);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    // ----- Read (all / by instructor) -----
    public async Task<List<CourseModel>> GetAllCoursesAsync()
    {
        var results = new List<CourseModel>();
        await foreach (var course in _table.QueryAsync<CourseModel>())
        {
            results.Add(course);
        }
        return results;
    }

    public async Task<List<CourseModel>> GetCoursesByInstructorAsync(string instructorId)
    {
        var results = new List<CourseModel>();
        await foreach (var course in _table.QueryAsync<CourseModel>(c => c.InstructorId == instructorId))
        {
            results.Add(course);
        }
        return results;
    }

    // ----- Update -----
    public async Task<bool> UpdateCourseAsync(string department, string courseCode, UpdateCourseRequest request)
    {
        var existing = await GetCourseAsync(department, courseCode);
        if (existing is null) return false;

        if (request.CourseName is not null) existing.CourseName = request.CourseName;
        if (request.InstructorId is not null) existing.InstructorId = request.InstructorId;
        if (request.InstructorName is not null) existing.InstructorName = request.InstructorName;
        if (request.Description is not null) existing.Description = request.Description;
        if (request.Capacity is not null) existing.Capacity = request.Capacity.Value;

        await _table.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Replace);
        return true;
    }

    /// <summary>Atomically bumps EnrolledCount by 1, retrying on ETag conflicts.</summary>
    public async Task<bool> TryIncrementEnrollmentAsync(string department, string courseCode)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var course = await GetCourseAsync(department, courseCode);
            if (course is null) return false;
            if (course.EnrolledCount >= course.Capacity) return false; // course full

            course.EnrolledCount += 1;
            try
            {
                await _table.UpdateEntityAsync(course, course.ETag, TableUpdateMode.Replace);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 412) // ETag mismatch, someone else updated first
            {
                // loop and retry with fresh data
            }
        }
        return false;
    }

    // ----- Delete -----
    public async Task<bool> DeleteCourseAsync(string department, string courseCode)
    {
        var response = await _table.DeleteEntityAsync(department, courseCode);
        return !response.IsError;
    }
}
