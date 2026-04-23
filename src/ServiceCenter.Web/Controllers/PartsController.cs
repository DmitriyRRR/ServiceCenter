using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Domain.Entities;
using ServiceCenter.Domain.Enums;
using ServiceCenter.Infrastructure.Data;
using ServiceCenter.Web.Models;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin,ServiceManager,Engineer")]
public class PartsController : Controller
{
    private readonly ApplicationDbContext _db;

    public PartsController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, PartStatus? status)
    {
        ViewData["Title"] = "Parts";
        ViewData["Search"] = search;
        ViewData["Status"] = status;

        var query = _db.Parts.AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.SKU != null && p.SKU.ToLower().Contains(term)) ||
                (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        var parts = await query.OrderBy(p => p.Name).ToListAsync();

        ViewBag.Statuses = Enum.GetValues<PartStatus>()
            .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString(), s == status))
            .ToList();

        return View(parts);
    }

    public async Task<IActionResult> Details(int id)
    {
        var part = await _db.Parts
            .Include(p => p.TicketParts).ThenInclude(tp => tp.Ticket).ThenInclude(t => t.Client)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (part == null) return NotFound();

        ViewData["Title"] = part.Name;
        return View(part);
    }

    [Authorize(Roles = "Admin,ServiceManager")]
    public IActionResult Create()
    {
        ViewData["Title"] = "New Part";
        PopulateStatusDropdown(null);
        return View(new PartFormViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ServiceManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PartFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "New Part";
            PopulateStatusDropdown(model.Status);
            return View(model);
        }

        var part = new Part
        {
            Name = model.Name,
            SKU = model.SKU,
            Description = model.Description,
            QuantityInStock = model.QuantityInStock,
            LowStockThreshold = model.LowStockThreshold,
            UnitPrice = model.UnitPrice,
            Status = model.Status
        };

        _db.Parts.Add(part);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Part \"{part.Name}\" created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,ServiceManager")]
    public async Task<IActionResult> Edit(int id)
    {
        var part = await _db.Parts.FindAsync(id);
        if (part == null) return NotFound();

        ViewData["Title"] = "Edit Part";
        PopulateStatusDropdown(part.Status);

        return View(new PartFormViewModel
        {
            Id = part.Id,
            Name = part.Name,
            SKU = part.SKU,
            Description = part.Description,
            QuantityInStock = part.QuantityInStock,
            LowStockThreshold = part.LowStockThreshold,
            UnitPrice = part.UnitPrice,
            Status = part.Status
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ServiceManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PartFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Edit Part";
            PopulateStatusDropdown(model.Status);
            return View(model);
        }

        var part = await _db.Parts.FindAsync(id);
        if (part == null) return NotFound();

        part.Name = model.Name;
        part.SKU = model.SKU;
        part.Description = model.Description;
        part.QuantityInStock = model.QuantityInStock;
        part.LowStockThreshold = model.LowStockThreshold;
        part.UnitPrice = model.UnitPrice;
        part.Status = model.Status;
        part.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Part \"{part.Name}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,ServiceManager")]
    public async Task<IActionResult> Delete(int id)
    {
        var part = await _db.Parts
            .Include(p => p.TicketParts)
            .Include(p => p.PartOrders)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (part == null) return NotFound();

        ViewData["Title"] = "Delete Part";
        return View(part);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin,ServiceManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var part = await _db.Parts.FindAsync(id);
        if (part == null) return NotFound();

        try
        {
            _db.Parts.Remove(part);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Part deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Cannot delete this part because it is used in tickets or orders. Remove those records first.";
            return RedirectToAction(nameof(Delete), new { id });
        }
    }

    private void PopulateStatusDropdown(PartStatus? selected)
    {
        ViewBag.Statuses = new SelectList(
            Enum.GetValues<PartStatus>().Select(s => new { Value = (int)s, Text = s.ToString() }),
            "Value", "Text", (int?)selected);
    }
}
