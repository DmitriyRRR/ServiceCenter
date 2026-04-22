using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin,ServiceManager")]
public class ClientsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Clients";
        return View();
    }
}
