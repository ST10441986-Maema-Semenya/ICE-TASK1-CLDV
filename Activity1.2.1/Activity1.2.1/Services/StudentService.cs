using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using UniversityEnrollment.Models;

namespace UniversityEnrollment.Services;

/// <summary>
/// Handles the connection to the "Students" table and all CRUD operations
/// against it. This is item (e) + (f) of the spec for the Students table.
/// </summary>
public class StudentService
{
    private readonly TableClient _table;

    public StudentService(IOptions<AzureStorageOptions> options)
    {
        var settings = options.Value;

        // (e) Establish the connection to the Azure Table storage account.
        var serviceClient = new TableServiceClient(settings.TableStorageConnectionString);
        _table = serviceClient.GetTableClient(settings.StudentsTableName);

        // Creates the "Students" table if it doesn't already exist.
        _table.CreateIfNotExists();
    }

    // ----- Create -----
    public async Task<StudentModel> AddStudentAsync(CreateStudentRequest request)
    {
        var entity = new StudentModel(request.StudentId)
        {
            Name = request.Name,
            Email = request.Email
        };

        await _table.AddEntityAsync(entity);
        return entity;
    }

    // ----- Read (single) -----
    public async Task<StudentModel?> GetStudentAsync(string studentId)
    {
        try
        {
            var response = await _table.GetEntityAsync<StudentModel>("Student", studentId);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    // ----- Read (all) -----
    public async Task<List<StudentModel>> GetAllStudentsAsync()
    {
        var results = new List<StudentModel>();
        await foreach (var student in _table.QueryAsync<StudentModel>(s => s.PartitionKey == "Student"))
        {
            results.Add(student);
        }
        return results;
    }

    /// <summary>Used by instructors to see the roster for one of their courses.</summary>
    public async Task<List<StudentModel>> GetStudentsEnrolledInCourseAsync(string department, string courseCode)
    {
        var all = await GetAllStudentsAsync();
        var key = $"{department}:{courseCode}";
        return all
            .Where(s => s.EnrolledCourseKeys.Split(';', StringSplitOptions.RemoveEmptyEntries).Contains(key))
            .ToList();
    }

    // ----- Update -----
    public async Task<bool> UpdateStudentAsync(string studentId, UpdateStudentRequest request)
    {
        var existing = await GetStudentAsync(studentId);
        if (existing is null) return false;

        if (request.Name is not null) existing.Name = request.Name;
        if (request.Email is not null) existing.Email = request.Email;

        await _table.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Replace);
        return true;
    }

    /// <summary>
    /// Adds a course to a student's enrolled list. Retries on ETag conflicts since
    /// this may run concurrently with other updates from the queue processor.
    /// </summary>
    public async Task<bool> AddCourseToStudentAsync(string studentId, string department, string courseCode)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var student = await GetStudentAsync(studentId);
            if (student is null) return false;

            student.AddEnrolledCourse(department, courseCode);
            try
            {
                await _table.UpdateEntityAsync(student, student.ETag, TableUpdateMode.Replace);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                // ETag conflict - reload and retry
            }
        }
        return false;
    }

    // ----- Delete -----
    public async Task<bool> DeleteStudentAsync(string studentId)
    {
        var response = await _table.DeleteEntityAsync("Student", studentId);
        return !response.IsError;
    }
}
