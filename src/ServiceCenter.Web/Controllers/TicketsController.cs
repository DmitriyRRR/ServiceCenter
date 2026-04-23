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
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, TicketStatus? status)
    {
        ViewData["Title"] = "Tickets";
        ViewData["Search"] = search;
        ViewData["Status"] = status;

        var query = _db.Tickets
            .Include(t => t.Client)
            .Include(t => t.Device)
            .Include(t => t.AssignedEngineer)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t =>
                t.TicketNumber.ToLower().Contains(term) ||
                t.Client.FirstName.ToLower().Contains(term) ||
                t.Client.LastName.ToLower().Contains(term) ||
                t.Device.Brand.ToLower().Contains(term) ||
                t.Device.Model.ToLower().Contains(term) ||
                t.ProblemDescription.ToLower().Contains(term));
        }

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        ViewBag.Statuses = Enum.GetValues<TicketStatus>()
            .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString(), s == status))
            .ToList();

        return View(tickets);
    }

    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Client)
            .Include(t => t.Device)
            .Include(t => t.AssignedEngineer)
            .Include(t => t.CreatedBy)
            .Include(t => t.WorkItems).ThenInclude(w => w.WorkType)
            .Include(t => t.WorkItems).ThenInclude(w => w.Engineer)
            .Include(t => t.TicketParts).ThenInclude(tp => tp.Part)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        ViewData["Title"] = ticket.TicketNumber;
        return View(ticket);
    }

    [Authorize(Roles = "Admin,ServiceManager")]
    public async Task<IActionResult> Create(int? clientId)
    {
        ViewData["Title"] = "New Ticket";
        await PopulateDropdowns(clientId, null, null);
        return View(new TicketFormViewModel { ClientId = clientId ?? 0 });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ServiceManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "New Ticket";
            await PopulateDropdowns(model.ClientId, model.DeviceId, model.AssignedEngineerId);
            return View(model);
        }

        var ticket = new Ticket
        {
            ClientId = model.ClientId,
            DeviceId = model.DeviceId,
            Status = TicketStatus.New,
            AssignedEngineerId = string.IsNullOrEmpty(model.AssignedEngineerId) ? null : model.AssignedEngineerId,
            CreatedById = _userManager.GetUserId(User)!,
            ProblemDescription = model.ProblemDescription,
            InternalNotes = model.InternalNotes,
            EstimatedTimeOut = model.EstimatedTimeOut,
            EstimatedPrice = model.EstimatedPrice,
            TicketNumber = string.Empty
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        ticket.TicketNumber = $"TKT-{ticket.CreatedAt:yyyyMMdd}-{ticket.Id:D4}";
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Ticket {ticket.TicketNumber} created.";
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    [Authorize(Roles = "Admin,ServiceManager")]
    public async Task<IActionResult> Edit(int id)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound();

        ViewData["Title"] = "Edit Ticket";
        await PopulateDropdowns(ticket.ClientId, ticket.DeviceId, ticket.AssignedEngineerId);

        return View(new TicketFormViewModel
        {
            Id = ticket.Id,
            ClientId = ticket.ClientId,
            DeviceId = ticket.DeviceId,
            Status = ticket.Status,
            AssignedEngineerId = ticket.AssignedEngineerId,
            ProblemDescription = ticket.ProblemDescription,
            InternalNotes = ticket.InternalNotes,
            EstimatedTimeOut = ticket.EstimatedTimeOut,
            ActualTimeOut = ticket.ActualTimeOut,
            EstimatedPrice = ticket.EstimatedPrice
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ServiceManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TicketFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Edit Ticket";
            await PopulateDropdowns(model.ClientId, model.DeviceId, model.AssignedEngineerId);
            return View(model);
        }

        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound();

        ticket.ClientId = model.ClientId;
        ticket.DeviceId = model.DeviceId;
        ticket.Status = model.Status;
        ticket.AssignedEngineerId = string.IsNullOrEmpty(model.AssignedEngineerId) ? null : model.AssignedEngineerId;
        ticket.ProblemDescription = model.ProblemDescription;
        ticket.InternalNotes = model.InternalNotes;
        ticket.EstimatedTimeOut = model.EstimatedTimeOut;
        ticket.ActualTimeOut = model.ActualTimeOut;
        ticket.EstimatedPrice = model.EstimatedPrice;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Ticket {ticket.TicketNumber} updated.";
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    [Authorize(Roles = "Admin,ServiceManager")]
    public async Task<IActionResult> Delete(int id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Client)
            .Include(t => t.Device)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        ViewData["Title"] = "Delete Ticket";
        return View(ticket);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin,ServiceManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound();

        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Ticket deleted.";
        return RedirectToAction(nameof(Index));
    }

    // Called via fetch() to populate the device dropdown when client changes
    [Authorize(Roles = "Admin,ServiceManager")]
    public async Task<IActionResult> GetDevicesByClient(int clientId)
    {
        var devices = await _db.Devices
            .Where(d => d.ClientId == clientId)
            .OrderBy(d => d.Brand).ThenBy(d => d.Model)
            .Select(d => new { d.Id, Name = d.Brand + " " + d.Model + (d.SerialNumber != null ? " (" + d.SerialNumber + ")" : "") })
            .ToListAsync();

        return Json(devices);
    }

    private async Task PopulateDropdowns(int? clientId, int? deviceId, string? engineerId)
    {
        var clients = await _db.Clients
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Select(c => new { c.Id, Name = c.LastName + " " + c.FirstName })
            .ToListAsync();
        ViewBag.Clients = new SelectList(clients, "Id", "Name", clientId);

        if (clientId.HasValue)
        {
            var devices = await _db.Devices
                .Where(d => d.ClientId == clientId.Value)
                .OrderBy(d => d.Brand).ThenBy(d => d.Model)
                .Select(d => new { d.Id, Name = d.Brand + " " + d.Model })
                .ToListAsync();
            ViewBag.Devices = new SelectList(devices, "Id", "Name", deviceId);
        }
        else
        {
            ViewBag.Devices = new SelectList(Enumerable.Empty<object>());
        }

        var engineers = await _userManager.GetUsersInRoleAsync("Engineer");
        ViewBag.Engineers = new SelectList(
            engineers.Where(e => e.IsActive).OrderBy(e => e.LastName),
            "Id", "UserName", engineerId);

        ViewBag.Statuses = new SelectList(
            Enum.GetValues<TicketStatus>().Select(s => new { Value = (int)s, Text = s.ToString() }),
            "Value", "Text");
    }
}
