using System.Threading.Tasks;
using BCrypt.Net;
using charolis.DAL;
using charolis.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace charolis.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly EfContext _context;
        public UserController(EfContext context) => _context = context;

        // GET: /User
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.AsNoTracking().ToListAsync();
            return View(users);
        }

        // GET: /User/Create
        public IActionResult Create()
        {
            ViewBag.Roles = new[] { "User", "Admin" };
            return View();
        }

        // POST: /User/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string username,
            string email,
            string phoneNumber,
            string address,
            string role,
            string password,
            string confirmPassword)
        {
            // Нормалізація
            username = username?.Trim() ?? "";
            email    = email?.Trim()    ?? "";
            phoneNumber = phoneNumber?.Trim();
            address     = address?.Trim();
            role        = string.IsNullOrWhiteSpace(role) ? "User" : role.Trim();
            password        = password        ?? "";
            confirmPassword = confirmPassword ?? "";

            // Валідація
            if (username == "")       ModelState.AddModelError("", "Логін обов’язковий.");
            if (email == "")          ModelState.AddModelError("", "Email обов’язковий.");
            if (password.Length < 6)  ModelState.AddModelError("", "Пароль мінімум 6 символів.");
            if (password != confirmPassword) ModelState.AddModelError("", "Паролі не збігаються.");
            if (await _context.Users.AnyAsync(u => u.Username == username))
                                      ModelState.AddModelError("", "Логін уже зайнятий.");
            if (await _context.Users.AnyAsync(u => u.Email == email))
                                      ModelState.AddModelError("", "Email уже використовується.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new[] { "User", "Admin" };
                return View();
            }

            var user = new User
            {
                Username     = username,
                Email        = email,
                PhoneNumber  = phoneNumber,
                Address      = address,
                Role         = role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /User/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            ViewBag.Roles = new[] { "User", "Admin" };
            return View(user);
        }

        // POST: /User/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string username,
            string email,
            string phoneNumber,
            string address,
            string role,
            string newPassword,
            string confirmNewPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Нормалізація
            username = username?.Trim() ?? "";
            email    = email?.Trim()    ?? "";
            phoneNumber = phoneNumber?.Trim();
            address     = address?.Trim();
            role        = string.IsNullOrWhiteSpace(role) ? user.Role : role.Trim();
            newPassword        = newPassword        ?? "";
            confirmNewPassword = confirmNewPassword ?? "";

            // Валідація
            if (username == "") ModelState.AddModelError("", "Логін обов’язковий.");
            if (email == "")    ModelState.AddModelError("", "Email обов’язковий.");

            if (newPassword != "")
            {
                if (newPassword.Length < 6) ModelState.AddModelError("", "Новий пароль мінімум 6 символів.");
                if (newPassword != confirmNewPassword) ModelState.AddModelError("", "Нові паролі не збігаються.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new[] { "User", "Admin" };
                return View(user);
            }

            // Оновлюємо поля
            user.Username   = username;
            user.Email      = email;
            user.PhoneNumber= phoneNumber;
            user.Address    = address;
            user.Role       = role;

            if (newPassword != "")
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /User/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: /User/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
