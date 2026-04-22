using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Client")]
public class MyTicketsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "My Repairs";
        return View();
    }
}
