# ServiceCenter

A web-based repair service center management system built with **ASP.NET Core MVC (.NET 9)**, **Entity Framework Core**, and **Bootstrap 5**.

---

## Features

| Module | Description |
|--------|-------------|
| **Tickets** | Full repair ticket lifecycle — create, assign to engineer, track status, record work and parts |
| **Clients** | Client directory with search; links to devices and ticket history |
| **Devices** | Device registry per client (brand, model, serial number) |
| **Parts** | Inventory management with stock levels, low-stock alerts, and status tracking |
| **Part Orders** | Order tracking with automatic stock update when an order is marked as Received |
| **Work Types** | Configurable catalog of repair services with default pricing; supports deactivation |
| **Users** | Identity-based user management; role assignment; soft-deactivation preserves history |

---

## Tech Stack

- **Framework:** ASP.NET Core MVC, .NET 9
- **ORM:** Entity Framework Core 9 + SQL Server (LocalDB for dev)
- **Auth:** ASP.NET Core Identity with role-based authorization
- **UI:** Bootstrap 5, Bootstrap Icons
- **Validation:** DataAnnotations + client-side via jQuery Unobtrusive Validation

---

## Project Structure

```
ServiceCenter/
├── src/
│   ├── ServiceCenter.Domain/          # Entities and enums
│   │   ├── Entities/                  # Client, Device, Ticket, Part, PartOrder, WorkType…
│   │   └── Enums/                     # TicketStatus, PartStatus, PartOrderStatus
│   ├── ServiceCenter.Infrastructure/  # EF Core DbContext, migrations, seeder
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   └── Migrations/
│   │   └── Seeds/DbSeeder.cs          # Roles, admin user, default work types
│   └── ServiceCenter.Web/             # MVC controllers and views
│       ├── Controllers/
│       ├── Models/                    # ViewModels (form models)
│       └── Views/
└── ServiceCenter.sln
```

---

## Roles

| Role | Access |
|------|--------|
| **Admin** | Full access to all modules including User management |
| **ServiceManager** | Clients, Devices, Tickets, Parts, Part Orders (read + write) |
| **Engineer** | Tickets (read), Parts (read), Part Orders (read), Work Types (read) |
| **Client** | My Repairs view only (own tickets) |

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server or SQL Server LocalDB

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/DmitriyRRR/ServiceCenter.git
   cd ServiceCenter
   ```

2. **Configure the connection string**

   Edit `src/ServiceCenter.Web/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ServiceCenterDb;Trusted_Connection=True;"
     }
   }
   ```

3. **Run the application**
   ```bash
   dotnet run --project src/ServiceCenter.Web
   ```

   On first start, EF Core migrations run automatically and the database is seeded with:
   - Roles: `Admin`, `ServiceManager`, `Engineer`, `Client`
   - Default admin account (see below)
   - Default work types (Diagnostics, Screen Repair, Soldering, etc.)

4. **Log in**

   | Field | Value |
   |-------|-------|
   | Email | `admin@servicecenter.com` |
   | Password | `Admin@123456` |

   **Change this password immediately after first login.**

---

## Key Design Decisions

- **No application service layer** — Controllers inject `ApplicationDbContext` directly. Simple and appropriate for this scope; can be extracted to an Application layer as the project grows.
- **Soft-delete for users** — Setting `IsActive = false` via Identity preserves all FK references in tickets and work items.
- **Ticket number format** — `TKT-YYYYMMDD-{Id:D4}` generated after the first save to include the database-assigned ID.
- **Part stock auto-update** — When a `PartOrder` is marked as `Received`, the controller increments `Part.QuantityInStock` and recalculates `Part.Status` automatically.
- **Cascade / Restrict FK behaviour** — `WorkItem` and `TicketPart` cascade on ticket delete. `Client → Ticket` and `Device → Ticket` are Restrict; controllers catch `DbUpdateException` and show a user-friendly error.

---

## Default Connection String

```
Server=(localdb)\mssqllocaldb;Database=ServiceCenterDb;Trusted_Connection=True;MultipleActiveResultSets=true
```
