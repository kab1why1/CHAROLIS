using charolis.DAL;
using charolis.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace charolis.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly EfContext _context;
    
    public OrderController(EfContext context) => _context = context;

    // Read
    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .ToListAsync();
        
        return View(orders);
    }
    
    private async Task LoadViewBags()
    {
        ViewBag.Users = await _context.Users
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Products = await _context.Products
            .AsNoTracking()
            .ToListAsync();
    }
    
    // Create
    [Authorize(Roles = "User,Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadViewBags();
        return View();
    }
    
    [Authorize(Roles = "User,Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int userId, int[] productIds, int[] quantities)
    {
        // Валідація користувача
        if (!await _context.Users.AnyAsync(u => u.Id == userId))
            ModelState.AddModelError("", "Оберіть коректного користувача.");

        // Валідація товарів
        if (productIds == null || productIds.Length == 0)
            ModelState.AddModelError("", "Виберіть хоча б один товар.");

        if (!ModelState.IsValid)
        {
            await LoadViewBags();
            return View();
        }

        // Створюємо замовлення
        var order = new Order
        {
            UserId    = userId,
            CreatedAt = DateTime.UtcNow,
            Items     = new List<OrderItem>()
        };

        for (int i = 0; i < productIds.Length; i++)
        {
            var pid = productIds[i];
            var qty = (i < quantities.Length && quantities[i] > 0) ? quantities[i] : 1;
            var prod = await _context.Products.FindAsync(pid);
            if (prod == null) 
                continue;

            order.Items.Add(new OrderItem
            {
                ProductId       = pid,
                Quantity        = qty,
                PriceAtPurchase = prod.Price
            });
        }

        if (order.Items.Count == 0)
        {
            ModelState.AddModelError("", "Жоден з обраних товарів не знайдений.");
            await LoadViewBags();
            return View();
        }

        // Підрахунок загальної суми
        order.Total = order.Items.Sum(x => x.Quantity * x.PriceAtPurchase);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Delete
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Delete(int id)
    {
        var order = _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefault(o => o.Id == id);

        if (order == null)
            return NotFound();

        return View(order);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var order = _context.Orders
            .Include(o => o.Items)
            .FirstOrDefault(o => o.Id == id);

        if (order != null)
        {
            _context.OrderItems.RemoveRange(order.Items); // обов'язково прибери дочірні елементи
            _context.Orders.Remove(order);
            _context.SaveChanges();
        }

        return RedirectToAction(nameof(Index));
    }
        
    // Edit
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        await LoadViewBags();
        ViewBag.OrderItems  = order.Items.ToDictionary(i => i.ProductId, i => i.Quantity);

        return View(order);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, int userId, int[] productIds, int[] quantities)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        // Валідація
        if (!await _context.Users.AnyAsync(u => u.Id == userId))
            ModelState.AddModelError("", "Оберіть коректного користувача.");
        if (productIds == null || productIds.Length == 0)
            ModelState.AddModelError("", "Виберіть хоча б один товар.");

        if (!ModelState.IsValid)
        {
            await LoadViewBags();
            ViewBag.OrderItems = order.Items.ToDictionary(i => i.ProductId, i => i.Quantity);
            return View(order);
        }

        // Оновлюємо користувача
        order.UserId = userId;

        // Видаляємо всі існуючі позиції
        _context.OrderItems.RemoveRange(order.Items);
        order.Items.Clear();

        // Додаємо знову за новим вибором
        for (int i = 0; i < productIds.Length; i++)
        {
            var pid = productIds[i];
            var qty = (i < quantities.Length && quantities[i] > 0) ? quantities[i] : 1;
            var prod = await _context.Products.FindAsync(pid);
            if (prod == null) continue;

            order.Items.Add(new OrderItem
            {
                ProductId       = pid,
                Quantity        = qty,
                PriceAtPurchase = prod.Price
            });
        }

        if (order.Items.Count == 0)
        {
            ModelState.AddModelError("", "Жоден з обраних товарів не знайдено.");
            await LoadViewBags();
            ViewBag.OrderItems = new Dictionary<int,int>();
            return View(order);
        }

        // Перерахунок суми
        order.Total = order.Items.Sum(x => x.Quantity * x.PriceAtPurchase);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    
}