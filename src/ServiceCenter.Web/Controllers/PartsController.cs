using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin,ServiceManager,Engineer")]
public class PartsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Parts";
        return View();
    }
}
