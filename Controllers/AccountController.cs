using Microsoft.AspNetCore.Mvc;
using Webbanhang.Helpers;
using Webbanhang.Models;

namespace Webbanhang.Controllers
{
    public class AccountController : Controller
    {
        private static readonly List<DemoAccount> Accounts = new()
        {
            new DemoAccount("admin@htpfood.vn", "Admin123", "Quản trị viên", "Admin"),
            new DemoAccount("user@htpfood.vn", "User123", "Khách hàng mẫu", "User")
        };

        public IActionResult Login(string? returnUrl = null)
        {
            if (AuthSession.IsLoggedIn(HttpContext))
            {
                return RedirectToRoleHome();
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var account = Accounts.FirstOrDefault(x =>
                string.Equals(x.Email, model.UserName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                x.Password == model.Password);

            if (account == null)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
                return View(model);
            }

            HttpContext.Session.SetString(AuthSession.UserNameKey, account.Email);
            HttpContext.Session.SetString(AuthSession.FullNameKey, account.FullName);
            HttpContext.Session.SetString(AuthSession.RoleKey, account.Role);

            TempData["Success"] = account.Role == "Admin"
                ? "Đăng nhập Admin thành công. Bạn có quyền quản lý sản phẩm và đơn hàng."
                : "Đăng nhập User thành công. Bạn có thể mua hàng và theo dõi đơn của mình.";

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToRoleHome();
        }

        public IActionResult Register()
        {
            if (AuthSession.IsLoggedIn(HttpContext))
            {
                return RedirectToRoleHome();
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            if (Accounts.Any(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã tồn tại.");
                return View(model);
            }

            var account = new DemoAccount(email, model.Password, model.FullName.Trim(), "User");
            Accounts.Add(account);

            HttpContext.Session.SetString(AuthSession.UserNameKey, account.Email);
            HttpContext.Session.SetString(AuthSession.FullNameKey, account.FullName);
            HttpContext.Session.SetString(AuthSession.RoleKey, account.Role);

            TempData["Success"] = "Đăng ký tài khoản User thành công.";
            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove(AuthSession.UserNameKey);
            HttpContext.Session.Remove(AuthSession.FullNameKey);
            HttpContext.Session.Remove(AuthSession.RoleKey);
            TempData["Success"] = "Đã đăng xuất.";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToRoleHome()
        {
            return AuthSession.IsAdmin(HttpContext)
                ? RedirectToAction("Orders", "Cart")
                : RedirectToAction("Index", "Product");
        }

        private record DemoAccount(string Email, string Password, string FullName, string Role);
    }
}
