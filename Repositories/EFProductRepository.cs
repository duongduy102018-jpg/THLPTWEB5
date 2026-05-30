using Microsoft.EntityFrameworkCore; 
using System.Collections.Generic;    
using System.Threading.Tasks;
using Webbanhang.Models;

namespace Webbanhang.Repositories
{
    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public EFProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category) // Eager loading Category
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id) // Thêm dấu ? vì sản phẩm có thể không tìm thấy (null)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            // Sửa lỗi logic: Kiểm tra null trước khi xóa để tránh crash ứng dụng
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}