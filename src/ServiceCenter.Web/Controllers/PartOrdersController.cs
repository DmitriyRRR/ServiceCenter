using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Domain.Entities;
using ServiceCenter.Domain.Enums;
using ServiceCenter.Infrastructure.Data;
using ServiceCenter.Web.Models;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin,ServiceManager,Engineer")]
public class PartOrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PartOrdersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, PartOrderStatus? status)
    {
        ViewData["Title"] = "Part Orders";
        ViewData["Search"] = search;
        ViewData["Status"] = status;

        var query = _db.PartOrders
            .Include(o => o.Part)
            .Include(o => o.CreatedBy)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(o =>
                o.Part.Name.ToLower().Contains(term) ||
                (o.Part.SKU != null && o.Part.SKU.ToLower().Contains(term)) ||
                (o.SupplierName != null && o.SupplierName.ToLower().Contains(term)));
        }

        var orders = await query.OrderByDescending(o => o.OrderedAt).ToListAsync();

        ViewBag.Statuses = Enum.GetValues<PartOrderStatus>()
            .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString(), s == status))
            .ToList();

        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.PartOrders
            .Include(o => o.Part)
            .Include(o => o.CreatedBy)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        ViewData["Title"] = $"Order #{order.Id}";
        return View(order);
    }

    [Authorize(Roles = "Admin,ServiceManager")]
    public async Task<IActionResult> Create(int? partId)
    {
        ViewData["Title"] = "New Order";
        await PopulatePartsDropdown(partId);
        return View(new PartOrderFormViewModel { PartId = partId ?? 0 });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ServiceManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PartOrderFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "New Order";
            await PopulatePartsDropdown(model.PartId);
            return View(model);
        }

        var order = new PartOrder
        {
            PartId = model.PartId,
            Quantity = model.Quantity,
            Status = model.Status,
            SupplierName = model.SupplierName,
            Notes = model.Notes,
            ExpectedAt = model.ExpectedAt,
            CreatedById = _userManager.GetUserId(User)
        };

        _db.PartOrders.Add(order);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Order #{order.Id} created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,ServiceManager")]
    public async Task<IActionResult> Edit(int id)
    {
        var order = await _db.PartOrders.FindAsync(id);
        if (order == null) return NotFound();

        ViewData["Title"] = $"Edit Order #{id}";
        await PopulatePartsDropdown(order.PartId);

        return View(new PartOrderFormViewModel
        {
            Id = order.Id,
            PartId = order.PartId,
            Quantity = order.Quantity,
            Status = order.Status,
            SupplierName = order.SupplierName,
            Notes = order.Notes,
            ExpectedAt = order.ExpectedAt,
            ReceivedAt = order.ReceivedAt
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ServiceManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PartOrderFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = $"Edit Order #{id}";
            await PopulatePartsDropdown(model.PartId);
            return View(model);
        }

        var order = await _db.PartOrders.Include(o => o.Part).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        bool justReceived = order.Status != PartOrderStatus.Received
                            && model.Status == PartOrderStatus.Received;

        order.PartId = model.PartId;
        order.Quantity = model.Quantity;
        order.Status = model.Status;
        order.SupplierName = model.SupplierName;
        order.Notes = model.Notes;
        order.ExpectedAt = model.ExpectedAt;
        order.ReceivedAt = model.ReceivedAt ?? (justReceived ? DateTime.UtcNow : null);

        if (justReceived)
        {
            order.Part.QuantityInStock += order.Quantity;
            order.Part.Status = order.Part.QuantityInStock == 0
                ? PartStatus.OutOfStock
                : order.Part.QuantityInStock <= order.Part.LowStockThreshold
                    ? PartStatus.LowStock
                    : PartStatus.InStock;
            order.Part.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = justReceived
            ? $"Order #{order.Id} marked as received — stock updated for \"{order.Part.Name}\"."
            : $"Order #{order.Id} updated.";

        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [Authorize(Roles = "Admin,ServiceManager")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.PartOrders
            .Include(o => o.Part)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        ViewData["Title"] = $"Delete Order #{id}";
        return View(order);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin,ServiceManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var order = await _db.PartOrders.FindAsync(id);
        if (order == null) return NotFound();

        _db.PartOrders.Remove(order);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Order #{id} deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulatePartsDropdown(int? selectedPartId)
    {
        var parts = await _db.Parts
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, Name = p.Name + (p.SKU != null ? $" ({p.SKU})" : "") })
            .ToListAsync();

        ViewBag.Parts = new SelectList(parts, "Id", "Name", selectedPartId);

        ViewBag.Statuses = new SelectList(
            Enum.GetValues<PartOrderStatus>().Select(s => new { Value = (int)s, Text = s.ToString() }),
            "Value", "Text");
    }
}
