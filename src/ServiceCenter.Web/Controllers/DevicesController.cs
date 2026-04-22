using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin,ServiceManager")]
public class DevicesController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Devices";
        return View();
    }
}
