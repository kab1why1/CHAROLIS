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
            username = username?.Trim() ?? "";
            email = email?.Trim() ?? "";
            phoneNumber = phoneNumber?.Trim();
            address = address?.Trim();
            role = string.IsNullOrWhiteSpace(role) ? "User" : role.Trim();
            password = password ?? "";
            confirmPassword = confirmPassword ?? "";

            if (username == "") ModelState.AddModelError("", "Логін обов’язковий.");
            if (email == "") ModelState.AddModelError("", "Email обов’язковий.");
            if (password.Length < 6) ModelState.AddModelError("", "Пароль мінімум 6 символів.");
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
                Username = username,
                Email = email,
                PhoneNumber = phoneNumber,
                Address = address,
                Role = role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /User/Edit or /User/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            User user;

            if (id == null)
            {
                // Редагування власного профілю
                var username = User.Identity?.Name;
                if (username == null) return Unauthorized();

                user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null) return NotFound();
            }
            else
            {
                // Адмін редагує користувача з id
                if (!User.IsInRole("Admin"))
                    return Forbid();

                user = await _context.Users.FindAsync(id.Value);
                if (user == null) return NotFound();
            }

            ViewBag.Roles = new[] { "User", "Admin" };
            return View(user);
        }

        // POST: /User/Edit or /User/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int? id,
            string username,
            string email,
            string phoneNumber,
            string address,
            string role,
            string newPassword,
            string confirmNewPassword)
        {
            var usernameCurrent = User.Identity?.Name;
            if (usernameCurrent == null) return Unauthorized();

            User user;

            if (id == null)
            {
                // Редагуємо власний профіль
                user = await _context.Users.FirstOrDefaultAsync(u => u.Username == usernameCurrent);
                if (user == null) return NotFound();

                // Звичайний користувач не може змінювати роль
                role = user.Role;
            }
            else
            {
                user = await _context.Users.FindAsync(id.Value);
                if (user == null) return NotFound();

                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == usernameCurrent);

                // Якщо користувач не адміністратор і редагує не свій профіль — заборонити
                if (!User.IsInRole("Admin") && currentUser.Id != user.Id)
                {
                    return Forbid();
                }
            }

            username = username?.Trim() ?? "";
            email = email?.Trim() ?? "";
            phoneNumber = phoneNumber?.Trim();
            address = address?.Trim();
            role = string.IsNullOrWhiteSpace(role) ? user.Role : role.Trim();
            newPassword = newPassword ?? "";
            confirmNewPassword = confirmNewPassword ?? "";

            if (username == "") ModelState.AddModelError("", "Логін обов’язковий.");
            if (email == "") ModelState.AddModelError("", "Email обов’язковий.");

            if (newPassword != "")
            {
                if (newPassword.Length < 6) ModelState.AddModelError("", "Новий пароль мінімум 6 символів.");
                if (newPassword != confirmNewPassword) ModelState.AddModelError("", "Нові паролі не збігаються.");
            }

            if (user.Username != username && await _context.Users.AnyAsync(u => u.Username == username))
                ModelState.AddModelError("", "Логін уже зайнятий.");
            if (user.Email != email && await _context.Users.AnyAsync(u => u.Email == email))
                ModelState.AddModelError("", "Email уже використовується.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new[] { "User", "Admin" };
                return View(user);
            }

            user.Username = username;
            user.Email = email;
            user.PhoneNumber = phoneNumber;
            user.Address = address;
            user.Role = role;

            if (newPassword != "")
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            if (id == null)
                return RedirectToAction(nameof(Profile));
            else
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

        // GET: /User/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var username = User.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return NotFound();

            return View(nameof(Profile), user);
        }
    }
}
