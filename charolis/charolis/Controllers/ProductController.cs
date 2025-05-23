using charolis.DAL;
using charolis.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace charolis.Controllers;

[Authorize]
public class ProductController : Controller
{
    private readonly EfContext _context;

    public ProductController(EfContext context) =>  _context = context;

    public async Task<IActionResult> Index() => View(await _context.Products.ToListAsync());
    
    // Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View();

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid) return View(product);
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    
    // Edit
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return BadRequest();
        }
        
        return View(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product updatedProduct)
    {
        if (id != updatedProduct.Id) return BadRequest();

        if (!ModelState.IsValid) return View(updatedProduct);

        _context.Products.Update(updatedProduct);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    
    // Delete
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        
        return View(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null) return NotFound();
        
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    
}