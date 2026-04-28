using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Domain.Entities;
using ServiceCenter.Infrastructure.Data;
using ServiceCenter.Web.Models;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin,ServiceManager")]
public class ClientsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ClientsController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewData["Title"] = "Clients";
        ViewData["Search"] = search;

        var query = _db.Clients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                c.Phone.Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)));
        }

        var clients = await query
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync();

        return View(clients);
    }

    public async Task<IActionResult> Details(int id)
    {
        var client = await _db.Clients
            .Include(c => c.Devices)
            .Include(c => c.Tickets)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null) return NotFound();

        ViewData["Title"] = $"{client.FirstName} {client.LastName}";
        return View(client);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "New Client";
        return View(new ClientFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "New Client";
            return View(model);
        }

        var client = new Client
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address,
            Notes = model.Notes
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Client {client.FirstName} {client.LastName} created.";
        return RedirectToAction(nameof(Details), new { id = client.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();

        ViewData["Title"] = "Edit Client";

        return View(new ClientFormViewModel
        {
            Id = client.Id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Phone = client.Phone,
            Email = client.Email,
            Address = client.Address,
            Notes = client.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClientFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Edit Client";
            return View(model);
        }

        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();

        client.FirstName = model.FirstName;
        client.LastName = model.LastName;
        client.Phone = model.Phone;
        client.Email = model.Email;
        client.Address = model.Address;
        client.Notes = model.Notes;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Client {client.FirstName} {client.LastName} updated.";
        return RedirectToAction(nameof(Details), new { id = client.Id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var client = await _db.Clients
            .Include(c => c.Devices)
            .Include(c => c.Tickets)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null) return NotFound();

        ViewData["Title"] = "Delete Client";
        return View(client);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();

        try
        {
            _db.Clients.Remove(client);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Client deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Cannot delete this client because they have linked tickets or devices. Remove them first.";
            return RedirectToAction(nameof(Delete), new { id });
        }
    }
}
