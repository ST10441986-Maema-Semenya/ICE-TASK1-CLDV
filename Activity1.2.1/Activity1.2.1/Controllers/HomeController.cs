using Microsoft.AspNetCore.Mvc;
using UniversityEnrollment.Services;

namespace UniversityEnrollment.Controllers;

public class HomeController : Controller
{
    private readonly CourseService _courseService;
    private readonly StudentService _studentService;

    public HomeController(CourseService courseService, StudentService studentService)
    {
        _courseService = courseService;
        _studentService = studentService;
    }

    public async Task<IActionResult> Index()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        var students = await _studentService.GetAllStudentsAsync();

        ViewBag.CourseCount = courses.Count;
        ViewBag.StudentCount = students.Count;
        ViewBag.OpenSeats = courses.Sum(c => Math.Max(0, c.Capacity - c.EnrolledCount));
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
