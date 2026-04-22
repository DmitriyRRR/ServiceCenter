using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin,ServiceManager,Engineer")]
public class TicketsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Tickets";
        return View();
    }

    [Authorize(Roles = "Admin,ServiceManager")]
    public IActionResult Create()
    {
        ViewData["Title"] = "New Ticket";
        return View();
    }
}
