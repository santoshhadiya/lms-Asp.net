using System.Security.Claims;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Books;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace College_Library_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    public class BooksController : Controller
    {
        private readonly IBookService _bookService;

        public BooksController(IBookService bookService)
        {
            _bookService = bookService;
        }

        public async Task<IActionResult> Index(BookListFilterViewModel filter)
        {
            var options = await _bookService.PopulateFormOptionsAsync();
            filter.CategoryOptions = options.CategoryOptions;
            filter.PublisherOptions = options.PublisherOptions;

            var books = await _bookService.GetBooksAsync(filter);
            ViewBag.Filter = filter;
            return View(books);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = await _bookService.PopulateFormOptionsAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await _bookService.PopulateFormOptionsAsync(model);
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message, bookId) = await _bookService.CreateBookAsync(model, userId, userEmail);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                await _bookService.PopulateFormOptionsAsync(model);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Details), new { id = bookId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _bookService.GetBookForEditAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Book not found in catalog.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BookFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await _bookService.PopulateFormOptionsAsync(model);
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message) = await _bookService.UpdateBookAsync(model, userId, userEmail);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                await _bookService.PopulateFormOptionsAsync(model);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Details), new { id = model.BookId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var model = await _bookService.GetBookDetailsAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Book not found in catalog.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message) = await _bookService.DeleteBookAsync(id, userId, userEmail);
            if (!success)
            {
                TempData["Error"] = message;
            }
            else
            {
                TempData["Success"] = message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
