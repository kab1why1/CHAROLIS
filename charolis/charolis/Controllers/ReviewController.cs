using charolis.DAL;
using charolis.Entities;
using charolis.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class ReviewController : Controller
{
    private readonly EfContext _context;
    public ReviewController(EfContext context) => _context = context;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewViewModel model)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("Details", "Product", new { id = model.ProductId });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var review = new Review
        {
            ProductId = model.ProductId,
            UserId = userId,
            Rating = model.Rating,
            Comment = model.Comment
        };
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return RedirectToAction("Details", "Product", new { id = model.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReviewViewModel model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var review = await _context.Reviews.FindAsync(model.Id);
        if (review == null) return NotFound();

        // перевірка доступу
        if (review.UserId != userId && !User.IsInRole("Admin"))
            return Forbid();

        review.Rating = model.Rating;
        review.Comment = model.Comment;
        await _context.SaveChangesAsync();
        return RedirectToAction("Details", "Product", new { id = review.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return NotFound();

        if (review.UserId != userId && !User.IsInRole("Admin"))
            return Forbid();

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return RedirectToAction("Details", "Product", new { id = review.ProductId });
    }
}