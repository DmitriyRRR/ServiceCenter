# WORK_README — ServiceCenter Project Context

> This file is written for a future Claude session (or any developer) resuming work on this project.
> It covers everything done so far, established conventions, pending work, and environment specifics.
> Last updated: 2026-04-23.

---

## What This Project Is

A **repair service center management web app** — think a small electronics repair shop.
Staff log repair tickets, track client devices, manage spare parts inventory, and record work done.

Built by the user (Dmitriy, GitHub: `DmitriyRRR`) together with Claude Code, starting from scratch.

**GitHub:** https://github.com/DmitriyRRR/ServiceCenter  
**Tech:** ASP.NET Core MVC, .NET 9, EF Core + SQL Server, Bootstrap 5 + Bootstrap Icons, ASP.NET Core Identity

---

## Architecture

```
ServiceCenter.Domain        →  Entities + Enums only. No logic.
ServiceCenter.Infrastructure →  DbContext, EF migrations, DbSeeder.
ServiceCenter.Web           →  MVC controllers, ViewModels (Models/), Razor views.
```

**No Application service layer.** Controllers inject `ApplicationDbContext` directly.
This was a deliberate choice for scope — fine for now, extractable later if the project grows.

`UserManager<ApplicationUser>` and `RoleManager<IdentityRole>` are also injected where needed (Tickets, PartOrders, WorkTypes, Users controllers).

---

## Domain Entities

| Entity | Key Fields | Relationships |
|---|---|---|
| `Client` | FirstName, LastName, Phone, Email?, Address?, Notes? | → Devices, → Tickets |
| `Device` | ClientId, Brand, Model, SerialNumber? | → Client, → Tickets |
| `Ticket` | TicketNumber, ClientId, DeviceId, Status, AssignedEngineerId, CreatedById, ProblemDescription, InternalNotes, TimeIn, EstimatedTimeOut, ActualTimeOut, EstimatedPrice, TotalPrice | → Client, Device, Engineer, CreatedBy, WorkItems, TicketParts |
| `WorkItem` | TicketId, WorkTypeId, Description?, Price, EngineerId? | → Ticket (cascade delete), → WorkType, → Engineer |
| `TicketPart` | TicketId, PartId, Quantity, UnitPriceAtTime | → Ticket (cascade delete), → Part; has computed `TotalPrice` |
| `Part` | Name, SKU?, QuantityInStock, LowStockThreshold, UnitPrice, Status | → TicketParts, → PartOrders |
| `PartOrder` | PartId, Quantity, Status, SupplierName?, ExpectedAt?, ReceivedAt?, CreatedById? | → Part, → CreatedBy |
| `WorkType` | Name, Description?, DefaultPrice, IsActive, CreatedById? | → WorkItems |
| `ApplicationUser` | (IdentityUser) FirstName, LastName, IsActive | → AssignedTickets, CreatedTickets, WorkItems |

### FK Delete Behaviours (important — affects what can be deleted)

| Relationship | Behaviour |
|---|---|
| Ticket → WorkItems | **Cascade** — deleting a ticket removes its work items |
| Ticket → TicketParts | **Cascade** — deleting a ticket removes its parts usage |
| Client → Tickets | **Restrict** — cannot delete client with tickets |
| Device → Tickets | **Restrict** — cannot delete device with tickets |
| Client → Devices | **Cascade** — deleting client cascades to devices |
| ApplicationUser → AssignedTickets | **SetNull** |
| ApplicationUser → CreatedTickets | **Restrict** |
| WorkItem → Engineer | **SetNull** |

All `Restrict` situations have `try/catch DbUpdateException` in the controller with a user-friendly `TempData["Error"]` message.

---

## Roles and Access Control

| Role | What they can do |
|---|---|
| **Admin** | Everything, including User management |
| **ServiceManager** | Clients, Devices, Tickets (all), Parts (all), PartOrders (all) |
| **Engineer** | Tickets (read + details), Parts (read), PartOrders (read), WorkTypes (read) |
| **Client** | MyTickets (own repair view only — stub, not implemented yet) |

Role access is set via `[Authorize(Roles = "...")]` at controller class level, with action-level overrides where write access is more restricted than read (e.g., TicketsController: class=Admin+ServiceManager+Engineer, Create/Edit/Delete=Admin+ServiceManager only).

---

## What Has Been Implemented (as of 2026-04-23)

All 7 main feature controllers are fully done with CRUD + views. Every one follows the same pattern:
`Index (search/filter) → Details → Create GET/POST → Edit GET/POST → Delete GET/POST`

### Controllers done

**ClientsController** (`[Authorize(Roles = "Admin,ServiceManager")]`)
- Index: search by name/phone/email
- Details: shows linked devices list + tickets list
- Delete: `DbUpdateException` catch (Restrict FK to Tickets)

**DevicesController** (`[Authorize(Roles = "Admin,ServiceManager")]`)
- Index: search + optional `?clientId=` filter (used when linking from Client details)
- Create: accepts optional `clientId` query param to pre-select client
- Fixed bug: Index query now includes `.Include(d => d.Tickets)` so ticket count shows correctly

**TicketsController** (`[Authorize(Roles = "Admin,ServiceManager,Engineer")]`, write=Admin+ServiceManager)
- Index: search + `TicketStatus` dropdown filter
- Create/Edit: dependent device dropdown — client dropdown change fires `fetch(/Tickets/GetDevicesByClient?clientId=X)` which returns JSON; JS repopulates device dropdown
- `TicketNumber` auto-generated after first save: `TKT-{CreatedAt:yyyyMMdd}-{Id:D4}`
- Details: full work items table + parts table with totals using `tp.TotalPrice` computed property
- Status field only shown on Edit (not Create — always starts as New)
- `UpdatedAt` is set on every Edit save

**PartsController** (`[Authorize(Roles = "Admin,ServiceManager,Engineer")]`, write=Admin+ServiceManager)
- Index: search + `PartStatus` dropdown filter; quantity cell turns amber when at or below threshold
- Details: shows "Used in Tickets" table with ticket number, client, qty, price per use
- `DbUpdateException` catch on delete

**PartOrdersController** (`[Authorize(Roles = "Admin,ServiceManager,Engineer")]`, write=Admin+ServiceManager)
- **Special behaviour on Edit:** when status changes from anything → `Received`:
  - `Part.QuantityInStock += order.Quantity`
  - `Part.Status` recalculated: 0 → OutOfStock, ≤ threshold → LowStock, > threshold → InStock
  - `Part.UpdatedAt = DateTime.UtcNow`
  - `order.ReceivedAt` auto-set to `DateTime.UtcNow` if left blank
- Delete: warns if order was already received (stock update is NOT reversed on delete)

**WorkTypesController** (`[Authorize(Roles = "Admin,Engineer")]`, write=Admin only)
- Index: search + "Active only" checkbox filter (default = active only)
- No Details view — list+edit is enough for a simple lookup table
- Delete: warns if work items exist, offers "Deactivate Instead" button as alternative
- `IsActive` checkbox enables soft-deactivation without deleting

**UsersController** (`[Authorize(Roles = "Admin")]`)
- Uses `UserManager<ApplicationUser>` + `RoleManager<IdentityRole>`, NOT DbContext directly
- Index: search + "Active only" filter; loads each user's role separately (N+1 — acceptable for small admin page)
- Create: registers via `_userManager.CreateAsync` + assigns role; password required
- Edit: updates profile fields + role reassignment + optional password reset via token
- **No hard delete** — "Delete" action is actually a soft-deactivate (`IsActive = false`) to preserve all FK references in Tickets and WorkItems
- Prevents deactivating your own account (checked in both GET and POST)

### ViewModels

All in `src/ServiceCenter.Web/Models/`:
`ClientFormViewModel`, `DeviceFormViewModel`, `TicketFormViewModel`, `PartFormViewModel`, `PartOrderFormViewModel`, `WorkTypeFormViewModel`, `UserFormViewModel`

### Layout / Shared

`Views/Shared/_Layout.cshtml` — sidebar navigation, role-conditional menu items, `TempData["Success"]`/`TempData["Error"]` alert banners, breadcrumb via `ViewData["Breadcrumb"]`.

---

## Stubs — NOT YET IMPLEMENTED

These controllers/views exist but are empty placeholders:

| Controller | Status | Notes |
|---|---|---|
| `HomeController` | Stub | Dashboard — could show open ticket count, low-stock alerts, recent activity |
| `MyTicketsController` | Stub | Client role view — shows only that client's own tickets |
| `AccountController` | **Done** | Login/Logout — fully working |

**WorkItem management** (adding work items to a ticket) and **TicketPart management** (adding parts to a ticket) are not implemented. These would likely be partial views or sub-actions inside Ticket Details, not separate top-level controllers.

---

## Established Conventions — Follow These

### Git workflow
- One feature branch per controller: `feature/{entity}-crud`
- Branch from `main`, implement, commit, push, then **review for bugs before merging**
- Merge with `--no-ff` to preserve branch history
- Commit message format: `feat:`, `fix:`, `docs:` prefix + description of the *why*
- Always add `Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>` to commits

### Code patterns
- Controllers: async throughout, `[ValidateAntiForgeryToken]` on all POST actions
- Always `try/catch DbUpdateException` in `DeleteConfirmed` when the entity has Restrict FK relationships
- Redirect to `Details` after Create/Edit (not Index), so the user sees what they just saved
- Redirect to `Index` after Delete
- `TempData["Success"]` on successful write, `TempData["Error"]` on caught exception
- `ViewData["Breadcrumb"]` set as raw HTML string in controller/view for the topbar

### Views
- Bootstrap 5 + Bootstrap Icons consistently
- Status badges: use local `@functions { static string StatusBadge(...) }` in each view rather than a shared helper
- Empty states: centred icon + message with link to Create
- Forms: max-width 580–680px card
- Delete views: `border-danger` card with red header; always show a warning if linked records exist
- `@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") }` on Create/Edit forms

---

## Environment Specifics

- **OS:** Windows 11 with WSL2 (Ubuntu). Project lives at `/mnt/c/Users/DR/source/repos/ServiceCenter`
- **Git:** Run all git commands with `GIT_DISCOVERY_ACROSS_FILESYSTEM=1` prefix from WSL, or just use Windows terminal / Git Bash directly
- **GitHub CLI (`gh`):** NOT installed in WSL. Use `curl` for GitHub API reads. For push: use HTTPS with PAT embedded in remote URL
- **GitHub remote URL pattern:** `https://{username}:{PAT}@github.com/DmitriyRRR/ServiceCenter.git`
- **PAT:** User provides it when needed. Do not store it in any file. Ask the user: "please share the PAT when you want to push"
- **Git identity in this repo:** `user.name = NoWayBack_2021`, `user.email = romanov.dmitriy.ua@gmail.com`

---

## Database

- **Provider:** SQL Server LocalDB (dev) — `(localdb)\mssqllocaldb`
- **Database name:** `ServiceCenterDb`
- **Migrations:** Run automatically on app start via `db.Database.MigrateAsync()` in `Program.cs`
- **Seeder** (`DbSeeder.cs`) runs on every start and is idempotent:
  - Creates roles: Admin, ServiceManager, Engineer, Client
  - Creates admin user: `admin@servicecenter.com` / `Admin@123456`
  - Creates 6 default work types (Diagnostics, Screen Repair, etc.)

---

## Potential Next Steps

In rough priority order:

1. **Dashboard** (`HomeController.Index`) — stats cards: open tickets count, low-stock parts count, today's received orders, recent tickets table
2. **WorkItem CRUD inside Ticket Details** — partial view or modal to add/edit/remove work items on a ticket; update `Ticket.TotalPrice` automatically
3. **TicketPart CRUD inside Ticket Details** — same pattern; deduct from `Part.QuantityInStock` on add
4. **MyTickets for Client role** (`MyTicketsController`) — filtered ticket list showing only tickets for the logged-in client's account
5. **Pagination** — Index views all load the full table; add simple page/size for large datasets
6. **Ticket status workflow** — enforce valid status transitions (e.g., can't go Closed → InProgress)
7. **Export** — CSV export of ticket list or parts inventory
