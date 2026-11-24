using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using practica415.Services;

namespace practica415.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController(IAccountService accounts) : Controller
{
    public IActionResult Index() => View(accounts.GetUsers());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SetRole(Guid id, string role)
    {
        var result = accounts.SetRole(id, role);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Роль пользователя изменена." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
