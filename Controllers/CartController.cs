using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webbanhang.Helpers;
using Webbanhang.Models;
using Webbanhang.Repositories;

namespace Webbanhang.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "HTPFoodCart";
        private const string CouponSessionKey = "HTPFoodCoupon";

        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;

        public CartController(IProductRepository productRepository, ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _context = context;
        }

        public IActionResult Index()
        {
            return View(BuildSummary());
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            AddProductToCart(product, quantity);

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";

            var referer = Request.Headers.Referer.ToString();

            return string.IsNullOrWhiteSpace(referer)
                ? RedirectToAction("Index", "Product")
                : Redirect(referer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int id, int quantity = 1)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            AddProductToCart(product, quantity);

            TempData["Success"] = "Sản phẩm đã được thêm vào giỏ hàng. Vui lòng kiểm tra giỏ hàng trước khi thanh toán.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
            }

            SaveCart(cart);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);
            HttpContext.Session.Remove(CouponSessionKey);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyCoupon(string? couponCode)
        {
            var code = couponCode?.Trim().ToUpperInvariant();
            var subtotal = GetCart().Sum(x => x.Total);

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["Error"] = "Vui lòng nhập mã giảm giá.";
            }
            else if (code == "FRESH10")
            {
                HttpContext.Session.SetString(CouponSessionKey, code);
                TempData["Success"] = "Áp dụng mã FRESH10 thành công: giảm 10%, tối đa 50.000 VNĐ.";
            }
            else if (code == "COMBO50" && subtotal >= 300000)
            {
                HttpContext.Session.SetString(CouponSessionKey, code);
                TempData["Success"] = "Áp dụng mã COMBO50 thành công: giảm 50.000 VNĐ.";
            }
            else if (code == "COMBO50")
            {
                TempData["Error"] = "Mã COMBO50 chỉ áp dụng cho đơn từ 300.000 VNĐ.";
            }
            else
            {
                TempData["Error"] = "Mã giảm giá không hợp lệ. Gợi ý: FRESH10 hoặc COMBO50.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove(CouponSessionKey);

            TempData["Success"] = "Đã bỏ mã giảm giá.";

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Checkout()
        {
            if (!AuthSession.IsLoggedIn(HttpContext))
            {
                TempData["Error"] = "Vui lòng đăng nhập tài khoản User trước khi đặt hàng.";

                return RedirectToAction(
                    "Login",
                    "Account",
                    new { returnUrl = Url.Action(nameof(Checkout), "Cart") }
                );
            }

            var summary = BuildSummary();

            if (!summary.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng đang trống.";
                return RedirectToAction(nameof(Index));
            }

            return View(summary);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(
            string customerName,
            string phone,
            string address,
            string paymentMethod,
            string? note)
        {
            if (!AuthSession.IsLoggedIn(HttpContext))
            {
                TempData["Error"] = "Vui lòng đăng nhập tài khoản User trước khi đặt hàng.";

                return RedirectToAction(
                    "Login",
                    "Account",
                    new { returnUrl = Url.Action(nameof(Checkout), "Cart") }
                );
            }

            var summary = BuildSummary();

            if (!summary.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng đang trống.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(customerName) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(address))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ họ tên, số điện thoại và địa chỉ giao hàng.";
                return RedirectToAction(nameof(Checkout));
            }

            var order = new Order
            {
                CustomerName = customerName.Trim(),
                Phone = phone.Trim(),
                Address = address.Trim(),
                Note = note?.Trim(),
                PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "COD" : paymentMethod,
                CouponCode = summary.CouponCode,
                Subtotal = summary.Subtotal,
                Discount = summary.Discount,
                ShippingFee = summary.ShippingFee,
                Total = summary.GrandTotal,
                Status = "Chờ xác nhận",
                CreatedAt = DateTime.Now,
                Items = summary.Items.Select(x => new OrderItem
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Price = x.Price,
                    Quantity = x.Quantity,
                    Total = x.Total
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            RememberOrderForCurrentUser(order.Id);

            HttpContext.Session.Remove(CartSessionKey);
            HttpContext.Session.Remove(CouponSessionKey);

            TempData["Success"] = $"Đặt hàng thành công! Mã đơn hàng của bạn là #{order.Id}.";

            return RedirectToAction(nameof(Success), new { id = order.Id });
        }

        public async Task<IActionResult> Success(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!CanViewOrder(id))
            {
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(order);
        }

        public async Task<IActionResult> Orders()
        {
            if (!AuthSession.IsLoggedIn(HttpContext))
            {
                TempData["Error"] = "Vui lòng đăng nhập để theo dõi đơn hàng.";

                return RedirectToAction(
                    "Login",
                    "Account",
                    new { returnUrl = Url.Action(nameof(Orders), "Cart") }
                );
            }

            ViewBag.IsAdmin = AuthSession.IsAdmin(HttpContext);

            IQueryable<Order> query = _context.Orders.Include(o => o.Items);

            if (!AuthSession.IsAdmin(HttpContext))
            {
                var ids = GetMyOrderIds();

                if (!ids.Any())
                {
                    return View(new List<Order>());
                }

                query = query.Where(o => ids.Contains(o.Id));
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            if (!AuthSession.IsAdmin(HttpContext))
            {
                TempData["Error"] = "User chỉ được theo dõi đơn hàng, không được sửa trạng thái đơn.";
                return RedirectToAction("AccessDenied", "Account");
            }

            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                order.Status = status;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật trạng thái đơn #{order.Id}.";

            return RedirectToAction(nameof(Orders));
        }

        private void AddProductToCart(Product product, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == product.Id);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    Quantity = Math.Max(1, quantity)
                });
            }
            else
            {
                item.Quantity += Math.Max(1, quantity);
            }

            SaveCart(cart);
        }

        private CartSummaryViewModel BuildSummary()
        {
            var items = GetCart();
            var subtotal = items.Sum(x => x.Total);
            var coupon = HttpContext.Session.GetString(CouponSessionKey);
            var discount = CalculateDiscount(coupon, subtotal);
            var shippingFee = subtotal == 0 || subtotal >= 500000 ? 0 : 25000;

            return new CartSummaryViewModel
            {
                Items = items,
                CouponCode = coupon,
                Subtotal = subtotal,
                Discount = discount,
                ShippingFee = shippingFee,
                GrandTotal = Math.Max(0, subtotal - discount + shippingFee)
            };
        }

        private static decimal CalculateDiscount(string? coupon, decimal subtotal)
        {
            if (string.IsNullOrWhiteSpace(coupon) || subtotal <= 0)
            {
                return 0;
            }

            return coupon.ToUpperInvariant() switch
            {
                "FRESH10" => Math.Min(subtotal * 0.10m, 50000m),
                "COMBO50" when subtotal >= 300000m => 50000m,
                _ => 0m
            };
        }

        private bool CanViewOrder(int orderId)
        {
            return AuthSession.IsAdmin(HttpContext) || GetMyOrderIds().Contains(orderId);
        }

        private void RememberOrderForCurrentUser(int orderId)
        {
            var ids = GetMyOrderIds();

            if (!ids.Contains(orderId))
            {
                ids.Add(orderId);
            }

            HttpContext.Session.SetString(
                AuthSession.MyOrderIdsKey,
                JsonSerializer.Serialize(ids)
            );
        }

        private List<int> GetMyOrderIds()
        {
            var json = HttpContext.Session.GetString(AuthSession.MyOrderIdsKey);

            return string.IsNullOrWhiteSpace(json)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);

            return string.IsNullOrEmpty(json)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(
                CartSessionKey,
                JsonSerializer.Serialize(cart)
            );
        }
    }
}