using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin,ServiceManager,Engineer")]
public class PartOrdersController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Part Orders";
        return View();
    }
}
