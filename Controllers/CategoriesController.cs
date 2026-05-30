using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Webbanhang.Models;
using Webbanhang.Repositories;
using Webbanhang.Helpers;

namespace Webbanhang.Controllers
{
    public class CategoriesController : Controller
    {
        // ĐÃ XÓA: IProductRepository vì không cần thiết dùng đến trong Category
        private readonly ICategoryRepository _categoryRepository;

        public CategoriesController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        private IActionResult? DenyIfNotAdmin()
        {
            if (AuthSession.IsAdmin(HttpContext)) return null;
            TempData["Error"] = "Chỉ Admin mới được quản lý danh mục.";
            return RedirectToAction("AccessDenied", "Account");
        }

        // Hiển thị danh sách danh mục
        public async Task<IActionResult> Index()
        {
            var denied = DenyIfNotAdmin();
            if (denied != null) return denied;

            var categories = await _categoryRepository.GetAllAsync();
            return View(categories); // Đổi tên biến thành số nhiều (categories) cho chuẩn ngữ nghĩa
        }

        // Hiển thị thông tin chi tiết danh mục
        public async Task<IActionResult> Display(int id)
        {
            var denied = DenyIfNotAdmin();
            if (denied != null) return denied;

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // Hiển thị form thêm danh mục mới
        public IActionResult Add()
        {
            var denied = DenyIfNotAdmin();
            if (denied != null) return denied;

            return View();
        }

        // Xử lý thêm danh mục mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Category category)
        {
            var denied = DenyIfNotAdmin();
            if (denied != null) return denied;

            if (ModelState.IsValid)
            {
                // SỬA LỖI: Thêm từ khóa 'await'
                await _categoryRepository.AddAsync(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // Hiển thị form cập nhật danh mục
        public async Task<IActionResult> Update(int id)
        {
            var denied = DenyIfNotAdmin();
            if (denied != null) return denied;

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // Xử lý cập nhật danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Category category)
        {
            var denied = DenyIfNotAdmin();
            if (denied != null) return denied;

            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // SỬA LỖI: Thêm từ khóa 'await'
                await _categoryRepository.UpdateAsync(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // Hiển thị form xác nhận xóa danh mục
        public async Task<IActionResult> Delete(int id)
        {
            var denied = DenyIfNotAdmin();
            if (denied != null) return denied;

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // Xử lý xóa danh mục
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var denied = DenyIfNotAdmin();
            if (denied != null) return denied;

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category != null)
            {
                // SỬA LỖI: Thêm từ khóa 'await'
                await _categoryRepository.DeleteAsync(id);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}