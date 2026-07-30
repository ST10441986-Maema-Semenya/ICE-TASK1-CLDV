using System.ComponentModel.DataAnnotations;

namespace UniversityEnrollment.Models;

// ----- View models for Razor pages (MVC) -----
// Kept separate from the API DTOs in Dtos.cs so form validation attributes
// don't leak into the wire contracts used by CourseService/StudentService callers.

public class CourseFormViewModel
{
    public bool IsNew { get; set; } = true;

    [Required, Display(Name = "Department code")]
    public string Department { get; set; } = string.Empty;

    [Required, Display(Name = "Course code")]
    public string CourseCode { get; set; } = string.Empty;

    [Required, Display(Name = "Course name")]
    public string CourseName { get; set; } = string.Empty;

    [Required, Display(Name = "Instructor ID")]
    public string InstructorId { get; set; } = string.Empty;

    [Required, Display(Name = "Instructor name")]
    public string InstructorName { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int Capacity { get; set; } = 30;
}

public class CourseDetailsViewModel
{
    public CourseDto Course { get; set; } = null!;
    public List<StudentDto> AllStudents { get; set; } = new();
    public string? SelectedStudentId { get; set; }
}

public class StudentFormViewModel
{
    public bool IsNew { get; set; } = true;

    [Required, Display(Name = "Student ID")]
    public string StudentId { get; set; } = string.Empty;

    [Required, Display(Name = "Full name")]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
