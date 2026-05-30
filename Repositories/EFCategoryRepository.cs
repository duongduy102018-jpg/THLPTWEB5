using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Webbanhang.Models;

namespace Webbanhang.Repositories
{
    public class EFCategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public EFCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            // Lấy danh sách tất cả các danh mục
            return await _context.Categories.ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            // Tìm danh mục theo ID
            return await _context.Categories.FindAsync(id);

            // LƯU Ý: Nếu sau này bạn muốn lấy 1 danh mục và bao gồm luôn 
            // danh sách các sản phẩm thuộc danh mục đó, bạn có thể dùng code sau:
            // return await _context.Categories
            //     .Include(c => c.Products) 
            //     .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            // Thêm kiểm tra null để tránh lỗi sập ứng dụng (NullReferenceException)
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}