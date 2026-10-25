using System.Security.Claims;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace College_Library_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    public class StudentsController : Controller
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        public async Task<IActionResult> Index(StudentListFilterViewModel filter)
        {
            filter.DepartmentOptions = await _studentService.GetDepartmentOptionsAsync();
            var students = await _studentService.GetStudentsAsync(filter);
            ViewBag.Filter = filter;
            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new StudentFormViewModel
            {
                DepartmentOptions = await _studentService.GetDepartmentOptionsAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.DepartmentOptions = await _studentService.GetDepartmentOptionsAsync();
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message, studentId) = await _studentService.CreateStudentAsync(model, userId, userEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                model.DepartmentOptions = await _studentService.GetDepartmentOptionsAsync();
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Details), new { id = studentId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _studentService.GetStudentForEditAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Student record not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StudentFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.DepartmentOptions = await _studentService.GetDepartmentOptionsAsync();
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message) = await _studentService.UpdateStudentAsync(model, userId, userEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                model.DepartmentOptions = await _studentService.GetDepartmentOptionsAsync();
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Details), new { id = model.StudentId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var model = await _studentService.GetStudentDetailsAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Student record not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message) = await _studentService.ToggleStudentStatusAsync(id, userId, userEmail);

            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
