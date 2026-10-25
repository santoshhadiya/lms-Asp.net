using System.Security.Claims;
using College_Library_Management_System.Models.Enums;
using College_Library_Management_System.Services.Interfaces;
using College_Library_Management_System.ViewModels.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace College_Library_Management_System.Controllers
{
    [Authorize(Roles = "Admin,Librarian")]
    public class BookCopiesController : Controller
    {
        private readonly IBookCopyService _copyService;
        private readonly IBookService _bookService;

        public BookCopiesController(IBookCopyService copyService, IBookService bookService)
        {
            _copyService = copyService;
            _bookService = bookService;
        }

        public async Task<IActionResult> Index(BookCopyListFilterViewModel filter)
        {
            filter.BookOptions = await _copyService.GetBookOptionsAsync();
            var copies = await _copyService.GetCopiesAsync(filter);
            ViewBag.Filter = filter;
            return View(copies);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? bookId)
        {
            var model = new BookCopyFormViewModel
            {
                BookId = bookId ?? 0,
                BookOptions = await _copyService.GetBookOptionsAsync()
            };

            if (bookId.HasValue && bookId.Value > 0)
            {
                var book = await _bookService.GetBookForEditAsync(bookId.Value);
                if (book != null)
                {
                    model.BookTitle = book.Title;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookCopyFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.BookOptions = await _copyService.GetBookOptionsAsync();
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message, copyId) = await _copyService.CreateCopyAsync(model, userId, userEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                model.BookOptions = await _copyService.GetBookOptionsAsync();
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction("Details", "Books", new { id = model.BookId });
        }

        [HttpGet]
        public async Task<IActionResult> CreateBatch(int bookId)
        {
            var book = await _bookService.GetBookForEditAsync(bookId);
            if (book == null)
            {
                TempData["Error"] = "Book not found.";
                return RedirectToAction("Index", "Books");
            }

            var model = new BatchCopyFormViewModel
            {
                BookId = bookId,
                BookTitle = book.Title,
                Prefix = "ACC-",
                StartingNumber = 1,
                NumberOfCopies = 5,
                ShelfLocation = "Stack A-1"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBatch(BatchCopyFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message, _) = await _copyService.CreateBatchCopiesAsync(model, userId, userEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction("Details", "Books", new { id = model.BookId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _copyService.GetCopyForEditAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Book copy not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BookCopyFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.BookOptions = await _copyService.GetBookOptionsAsync();
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message) = await _copyService.UpdateCopyAsync(model, userId, userEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                model.BookOptions = await _copyService.GetBookOptionsAsync();
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            var copy = await _copyService.GetCopyByIdAsync(id);
            if (copy == null)
            {
                TempData["Error"] = "Book copy not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new ChangeCopyStatusViewModel
            {
                BookCopyId = copy.BookCopyId,
                AccessionNumber = copy.AccessionNumber,
                BookTitle = copy.Book.Title,
                CurrentStatus = copy.Status,
                NewStatus = copy.Status,
                ConditionNotes = copy.ConditionNotes
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(ChangeCopyStatusViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var (success, message) = await _copyService.UpdateCopyStatusAsync(
                model.BookCopyId,
                model.NewStatus,
                model.ConditionNotes,
                userId,
                userEmail);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> History(int id)
        {
            var history = await _copyService.GetCopyHistoryAsync(id);
            if (history == null)
            {
                TempData["Error"] = "Book copy not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(history);
        }
    }
}
