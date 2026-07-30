using Microsoft.AspNetCore.Mvc;
using UniversityEnrollment.Models;
using UniversityEnrollment.Services;

namespace UniversityEnrollment.Controllers;

public class StudentsController : Controller
{
    private readonly StudentService _studentService;
    private readonly CourseService _courseService;

    public StudentsController(StudentService studentService, CourseService courseService)
    {
        _studentService = studentService;
        _courseService = courseService;
    }

    // GET /Students
    public async Task<IActionResult> Index()
    {
        var students = await _studentService.GetAllStudentsAsync();
        return View(students.Select(ToDto).OrderBy(s => s.Name).ToList());
    }

    // GET /Students/Details/S001
    // A student's own dashboard - the courses they're enrolled in.
    public async Task<IActionResult> Details(string id)
    {
        var student = await _studentService.GetStudentAsync(id);
        if (student is null) return NotFound();

        var enrolledKeys = student.GetEnrolledCourses();
        var courses = new List<CourseDto>();
        foreach (var (dept, code) in enrolledKeys)
        {
            var course = await _courseService.GetCourseAsync(dept, code);
            if (course is not null) courses.Add(CoursesController.ToDto(course));
        }

        ViewBag.EnrolledCourses = courses.OrderBy(c => c.Department).ThenBy(c => c.CourseCode).ToList();
        return View(ToDto(student));
    }

    // GET /Students/Create
    [HttpGet]
    public IActionResult Create() => View(new StudentFormViewModel());

    // POST /Students/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var existing = await _studentService.GetStudentAsync(model.StudentId);
        if (existing is not null)
        {
            ModelState.AddModelError(string.Empty, "A student with this ID already exists.");
            return View(model);
        }

        await _studentService.AddStudentAsync(new CreateStudentRequest(model.StudentId, model.Name, model.Email));
        TempData["Success"] = $"Student {model.StudentId} created.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Students/Edit/S001
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var student = await _studentService.GetStudentAsync(id);
        if (student is null) return NotFound();

        return View(new StudentFormViewModel
        {
            IsNew = false,
            StudentId = student.RowKey,
            Name = student.Name,
            Email = student.Email
        });
    }

    // POST /Students/Edit/S001
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, StudentFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var updated = await _studentService.UpdateStudentAsync(id, new UpdateStudentRequest(model.Name, model.Email));
        if (!updated) return NotFound();

        TempData["Success"] = $"Student {id} updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Students/Delete/S001
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _studentService.DeleteStudentAsync(id);
        TempData["Success"] = $"Student {id} deleted.";
        return RedirectToAction(nameof(Index));
    }

    internal static StudentDto ToDto(StudentModel s) => new(
        s.RowKey,
        s.Name,
        s.Email,
        s.GetEnrolledCourses().Select(c => $"{c.CoursePartitionKey}:{c.CourseRowKey}").ToList());
}
