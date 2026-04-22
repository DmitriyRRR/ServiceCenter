using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin,Engineer")]
public class WorkTypesController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Work Types";
        return View();
    }
}
