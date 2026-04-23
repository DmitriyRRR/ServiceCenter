using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Domain.Entities;
using ServiceCenter.Infrastructure.Data;
using ServiceCenter.Web.Models;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin,Engineer")]
public class WorkTypesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public WorkTypesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, bool? activeOnly)
    {
        ViewData["Title"] = "Work Types";
        ViewData["Search"] = search;
        ViewData["ActiveOnly"] = activeOnly ?? true;

        var query = _db.WorkTypes.AsQueryable();

        if (activeOnly != false)
            query = query.Where(w => w.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(w =>
                w.Name.ToLower().Contains(term) ||
                (w.Description != null && w.Description.ToLower().Contains(term)));
        }

        var workTypes = await query.OrderBy(w => w.Name).ToListAsync();
        return View(workTypes);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        ViewData["Title"] = "New Work Type";
        return View(new WorkTypeFormViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "New Work Type";
            return View(model);
        }

        var workType = new WorkType
        {
            Name = model.Name,
            Description = model.Description,
            DefaultPrice = model.DefaultPrice,
            IsActive = model.IsActive,
            CreatedById = _userManager.GetUserId(User)
        };

        _db.WorkTypes.Add(workType);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Work type \"{workType.Name}\" created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var workType = await _db.WorkTypes.FindAsync(id);
        if (workType == null) return NotFound();

        ViewData["Title"] = "Edit Work Type";
        return View(new WorkTypeFormViewModel
        {
            Id = workType.Id,
            Name = workType.Name,
            Description = workType.Description,
            DefaultPrice = workType.DefaultPrice,
            IsActive = workType.IsActive
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WorkTypeFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Edit Work Type";
            return View(model);
        }

        var workType = await _db.WorkTypes.FindAsync(id);
        if (workType == null) return NotFound();

        workType.Name = model.Name;
        workType.Description = model.Description;
        workType.DefaultPrice = model.DefaultPrice;
        workType.IsActive = model.IsActive;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Work type \"{workType.Name}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var workType = await _db.WorkTypes
            .Include(w => w.WorkItems)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workType == null) return NotFound();

        ViewData["Title"] = "Delete Work Type";
        return View(workType);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var workType = await _db.WorkTypes.FindAsync(id);
        if (workType == null) return NotFound();

        try
        {
            _db.WorkTypes.Remove(workType);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Work type deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Cannot delete this work type because it is used in existing work items. Deactivate it instead.";
            return RedirectToAction(nameof(Delete), new { id });
        }
    }
}
