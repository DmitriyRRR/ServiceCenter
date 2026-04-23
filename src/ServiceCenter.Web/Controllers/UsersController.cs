using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiceCenter.Domain.Entities;
using ServiceCenter.Web.Models;

namespace ServiceCenter.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    private static readonly string[] Roles = ["Admin", "ServiceManager", "Engineer", "Client"];

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index(string? search, bool? activeOnly)
    {
        ViewData["Title"] = "Users";
        ViewData["Search"] = search;
        ViewData["ActiveOnly"] = activeOnly ?? true;

        var query = _userManager.Users.AsQueryable();

        if (activeOnly != false)
            query = query.Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        var users = await query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();

        var userRoles = new Dictionary<string, string>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.Id] = roles.FirstOrDefault() ?? "—";
        }

        ViewBag.UserRoles = userRoles;
        return View(users);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "New User";
        PopulateRolesDropdown(null);
        return View(new UserFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Password is required when creating a user.");

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "New User";
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            IsActive = model.IsActive,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewData["Title"] = "New User";
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Role);

        TempData["Success"] = $"User {user.Email} created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        ViewData["Title"] = "Edit User";
        PopulateRolesDropdown(roles.FirstOrDefault());

        return View(new UserFormViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = roles.FirstOrDefault() ?? string.Empty,
            IsActive = user.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Edit User";
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.IsActive = model.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewData["Title"] = "Edit User";
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        // Update role
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (!passResult.Succeeded)
            {
                foreach (var error in passResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                ViewData["Title"] = "Edit User";
                PopulateRolesDropdown(model.Role);
                return View(model);
            }
        }

        TempData["Success"] = $"User {user.Email} updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Prevent deleting the currently logged-in admin
        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.UserRole = roles.FirstOrDefault() ?? "—";
        ViewData["Title"] = "Deactivate User";
        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = $"User {user.Email} has been deactivated.";
        return RedirectToAction(nameof(Index));
    }

    private void PopulateRolesDropdown(string? selected)
    {
        ViewBag.Roles = new SelectList(Roles.Select(r => new { Value = r, Text = r }), "Value", "Text", selected);
    }
}
