using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using charolis.DAL;
using charolis.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace charolis.Controllers;

public class AccountController : Controller
{
    private readonly EfContext _context;
    public AccountController(EfContext context) =>  _context = context;
    
    
    //Register
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string username,
        string email,
        string password,
        string confirmPassword,
        string? phoneNumber,
        string? address)
    {
        
        // required 
        if (string.IsNullOrWhiteSpace(username))
            ModelState.AddModelError("", "Логін обов’язковий.");
        if (string.IsNullOrWhiteSpace(email))
            ModelState.AddModelError("", "Email обов’язковий.");
        if (string.IsNullOrWhiteSpace(password))
            ModelState.AddModelError("", "Пароль обов’язковий.");
        if (string.IsNullOrWhiteSpace(confirmPassword))
            ModelState.AddModelError("", "Підтвердіть пароль.");
        
        // validate ?
        
        if (!string.IsNullOrWhiteSpace(email) && !new EmailAddressAttribute().IsValid(email))
            ModelState.AddModelError("", "Неправильний формат Email.");
        if (password.Length < 6)
            ModelState.AddModelError("", "Пароль має бути мінімум 6 символів.");
        if (password != confirmPassword)
            ModelState.AddModelError("", "Паролі не збігаються.");
        if (!string.IsNullOrWhiteSpace(phoneNumber) && !new PhoneAttribute().IsValid(phoneNumber))
            ModelState.AddModelError("", "Неправильний формат телефону.");
        if (address?.Length > 200)
            ModelState.AddModelError("", "Адреса занадто довга (макс. 200 символів).");
        
        // unic
            
        if (await _context.Users.AnyAsync(u => u.Username == username))
            ModelState.AddModelError("", "Цей логін уже зайнятий.");
        if (await _context.Users.AnyAsync(u => u.Email == email))
            ModelState.AddModelError("", "Цей Email уже використовується.");
        
        if(!ModelState.IsValid)
            return  View();

        var user = new User
        {
            Username = username.Trim(),
            Email = email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            PhoneNumber = phoneNumber?.Trim(),
            Address = address?.Trim(),
            Role = "User"
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        //return RedirectToAction("Index", "Home");
        return RedirectToAction("Login", "Account");
    }
    
    // Login
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            ModelState.AddModelError("", "wrond login or password.");
            return View();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var identify = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identify);
        await HttpContext.SignInAsync(principal);
        
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
    
    public IActionResult AccessDenied() => View();
}