using Microsoft.AspNetCore.Mvc;
using UniversityEnrollment.Models;
using UniversityEnrollment.Services;

namespace UniversityEnrollment.Controllers;

public class CoursesController : Controller
{
    private readonly CourseService _courseService;
    private readonly StudentService _studentService;
    private readonly EnrollmentQueueSender _queueSender;

    public CoursesController(CourseService courseService, StudentService studentService, EnrollmentQueueSender queueSender)
    {
        _courseService = courseService;
        _studentService = studentService;
        _queueSender = queueSender;
    }

    // GET /Courses
    // Students browse all available courses here.
    public async Task<IActionResult> Index()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        return View(courses.Select(ToDto).OrderBy(c => c.Department).ThenBy(c => c.CourseCode).ToList());
    }

    // GET /Courses/Details/CS/CS101
    // Shows course info plus the enrol form.
    [HttpGet("Courses/Details/{department}/{courseCode}")]
    public async Task<IActionResult> Details(string department, string courseCode)
    {
        var course = await _courseService.GetCourseAsync(department, courseCode);
        if (course is null) return NotFound();

        var students = await _studentService.GetAllStudentsAsync();

        var vm = new CourseDetailsViewModel
        {
            Course = ToDto(course),
            AllStudents = students.Select(StudentsController.ToDto).OrderBy(s => s.Name).ToList()
        };
        return View(vm);
    }

    // GET /Courses/Roster/CS/CS101
    // Instructors view the enrolled students for one of their courses.
    [HttpGet("Courses/Roster/{department}/{courseCode}")]
    public async Task<IActionResult> Roster(string department, string courseCode)
    {
        var course = await _courseService.GetCourseAsync(department, courseCode);
        if (course is null) return NotFound();

        var students = await _studentService.GetStudentsEnrolledInCourseAsync(department, courseCode);
        ViewBag.Course = ToDto(course);
        return View(students.Select(StudentsController.ToDto).OrderBy(s => s.Name).ToList());
    }

    // GET /Courses/Create
    [HttpGet]
    public IActionResult Create() => View(new CourseFormViewModel());

    // POST /Courses/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var existing = await _courseService.GetCourseAsync(model.Department, model.CourseCode);
        if (existing is not null)
        {
            ModelState.AddModelError(string.Empty, "A course with this department/code already exists.");
            return View(model);
        }

        await _courseService.AddCourseAsync(new CreateCourseRequest(
            model.Department, model.CourseCode, model.CourseName,
            model.InstructorId, model.InstructorName, model.Description, model.Capacity));

        TempData["Success"] = $"Course {model.Department}{model.CourseCode} created.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Courses/Edit/CS/CS101
    [HttpGet("Courses/Edit/{department}/{courseCode}")]
    public async Task<IActionResult> Edit(string department, string courseCode)
    {
        var course = await _courseService.GetCourseAsync(department, courseCode);
        if (course is null) return NotFound();

        return View(new CourseFormViewModel
        {
            IsNew = false,
            Department = course.PartitionKey,
            CourseCode = course.RowKey,
            CourseName = course.CourseName,
            InstructorId = course.InstructorId,
            InstructorName = course.InstructorName,
            Description = course.Description,
            Capacity = course.Capacity
        });
    }

    // POST /Courses/Edit/CS/CS101
    [HttpPost("Courses/Edit/{department}/{courseCode}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string department, string courseCode, CourseFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var updated = await _courseService.UpdateCourseAsync(department, courseCode, new UpdateCourseRequest(
            model.CourseName, model.InstructorId, model.InstructorName, model.Description, model.Capacity));

        if (!updated) return NotFound();

        TempData["Success"] = $"Course {department}{courseCode} updated.";
        return RedirectToAction(nameof(Details), new { department, courseCode });
    }

    // POST /Courses/Delete/CS/CS101
    [HttpPost("Courses/Delete/{department}/{courseCode}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string department, string courseCode)
    {
        await _courseService.DeleteCourseAsync(department, courseCode);
        TempData["Success"] = $"Course {department}{courseCode} deleted.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Courses/Enroll
    // A student submits this from the course Details page; it queues the
    // request instead of writing to the Students table directly.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(string studentId, string department, string courseCode)
    {
        var course = await _courseService.GetCourseAsync(department, courseCode);
        var student = await _studentService.GetStudentAsync(studentId);

        if (course is null || student is null)
        {
            TempData["Error"] = "Course or student not found.";
            return RedirectToAction(nameof(Details), new { department, courseCode });
        }

        if (course.EnrolledCount >= course.Capacity)
        {
            TempData["Error"] = "This course is at full capacity.";
            return RedirectToAction(nameof(Details), new { department, courseCode });
        }

        await _queueSender.EnqueueEnrollmentAsync(studentId, department, courseCode);
        TempData["Success"] = $"Enrolment request received for {student.Name} - it will show up on their record shortly.";
        return RedirectToAction(nameof(Details), new { department, courseCode });
    }

    internal static CourseDto ToDto(CourseModel c) => new(
        c.PartitionKey, c.RowKey, c.CourseName, c.InstructorId, c.InstructorName,
        c.Description, c.Capacity, c.EnrolledCount);
}
