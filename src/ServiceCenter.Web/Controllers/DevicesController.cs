using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Domain.Entities;
using ServiceCenter.Infrastructure.Data;
using ServiceCenter.Web.Models;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin,ServiceManager")]
public class DevicesController : Controller
{
    private readonly ApplicationDbContext _db;

    public DevicesController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, int? clientId)
    {
        ViewData["Title"] = "Devices";
        ViewData["Search"] = search;
        ViewData["ClientId"] = clientId;

        var query = _db.Devices.Include(d => d.Client).Include(d => d.Tickets).AsQueryable();

        if (clientId.HasValue)
            query = query.Where(d => d.ClientId == clientId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(d =>
                d.Brand.ToLower().Contains(term) ||
                d.Model.ToLower().Contains(term) ||
                (d.SerialNumber != null && d.SerialNumber.ToLower().Contains(term)) ||
                d.Client.FirstName.ToLower().Contains(term) ||
                d.Client.LastName.ToLower().Contains(term));
        }

        var devices = await query
            .OrderBy(d => d.Client.LastName).ThenBy(d => d.Brand).ThenBy(d => d.Model)
            .ToListAsync();

        ViewBag.ClientFilter = clientId.HasValue
            ? await _db.Clients.Where(c => c.Id == clientId.Value)
                .Select(c => c.FirstName + " " + c.LastName).FirstOrDefaultAsync()
            : null;

        return View(devices);
    }

    public async Task<IActionResult> Details(int id)
    {
        var device = await _db.Devices
            .Include(d => d.Client)
            .Include(d => d.Tickets)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null) return NotFound();

        ViewData["Title"] = $"{device.Brand} {device.Model}";
        return View(device);
    }

    public async Task<IActionResult> Create(int? clientId)
    {
        ViewData["Title"] = "New Device";
        await PopulateClientsDropdown(clientId);
        return View(new DeviceFormViewModel { ClientId = clientId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeviceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "New Device";
            await PopulateClientsDropdown(model.ClientId);
            return View(model);
        }

        var device = new Device
        {
            ClientId = model.ClientId,
            Brand = model.Brand,
            Model = model.Model,
            SerialNumber = model.SerialNumber,
            Description = model.Description
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Device {device.Brand} {device.Model} created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var device = await _db.Devices.FindAsync(id);
        if (device == null) return NotFound();

        ViewData["Title"] = "Edit Device";
        await PopulateClientsDropdown(device.ClientId);

        return View(new DeviceFormViewModel
        {
            Id = device.Id,
            ClientId = device.ClientId,
            Brand = device.Brand,
            Model = device.Model,
            SerialNumber = device.SerialNumber,
            Description = device.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DeviceFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Edit Device";
            await PopulateClientsDropdown(model.ClientId);
            return View(model);
        }

        var device = await _db.Devices.FindAsync(id);
        if (device == null) return NotFound();

        device.ClientId = model.ClientId;
        device.Brand = model.Brand;
        device.Model = model.Model;
        device.SerialNumber = model.SerialNumber;
        device.Description = model.Description;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Device {device.Brand} {device.Model} updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var device = await _db.Devices
            .Include(d => d.Client)
            .Include(d => d.Tickets)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null) return NotFound();

        ViewData["Title"] = "Delete Device";
        return View(device);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var device = await _db.Devices.FindAsync(id);
        if (device == null) return NotFound();

        try
        {
            _db.Devices.Remove(device);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Device deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Cannot delete this device because it has linked tickets. Remove them first.";
            return RedirectToAction(nameof(Delete), new { id });
        }
    }

    private async Task PopulateClientsDropdown(int? selectedClientId)
    {
        var clients = await _db.Clients
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Select(c => new { c.Id, Name = c.LastName + " " + c.FirstName })
            .ToListAsync();

        ViewBag.Clients = new SelectList(clients, "Id", "Name", selectedClientId);
    }
}
