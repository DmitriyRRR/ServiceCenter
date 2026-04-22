using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Users";
        return View();
    }
}
