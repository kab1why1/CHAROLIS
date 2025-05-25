using System.Threading.Tasks;
using charolis.DAL;
using charolis.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using charolis.Models;

namespace charolis.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly EfContext _context;

        public CartController(EfContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var model = new CartViewModel
            {
                Balance = user.Balance,
                Orders = orders
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Pay(int orderId)
        {
            var username = User.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);
            if (order == null) return NotFound();

            if (order.IsPaid)
            {
                TempData["Message"] = "Це замовлення вже оплачено.";
                return RedirectToAction(nameof(Index));
            }

            if (user.Balance < order.Total)
            {
                TempData["Error"] = "Недостатньо коштів для оплати.";
                return RedirectToAction(nameof(Index));
            }

            user.Balance -= order.Total;
            order.IsPaid = true;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Замовлення оплачено успішно.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CancelPayment(int orderId)
        {
            var username = User.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);
            if (order == null) return NotFound();

            if (!order.IsPaid)
            {
                TempData["Message"] = "Це замовлення не оплачено.";
                return RedirectToAction(nameof(Index));
            }

            user.Balance += order.Total;
            order.IsPaid = false;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Оплата скасована, кошти повернено на баланс.";
            return RedirectToAction(nameof(Index));
        }
    }
}
