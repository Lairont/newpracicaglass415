using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using practica415.Models;
using practica415.Services;

namespace practica415.Controllers;

[Authorize]
public sealed class FitnessController(IAccountService accounts) : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole("Manager") || User.IsInRole("Admin"))
        {
            var clients = accounts.GetClients();
            return View("Staff", new FitnessDashboardViewModel { Clients = clients, TemporaryPassword = TempData["TemporaryPassword"]?.ToString(), Client = Guid.TryParse(TempData["NewClientId"]?.ToString(), out var id) ? accounts.GetById(id) : null });
        }
        var clientId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        return View("Index", new FitnessDashboardViewModel { Client = accounts.GetById(clientId) });
    }

    [Authorize(Roles = "Manager,Admin"), HttpGet]
    public IActionResult CreateClient() => View(new ClientFormViewModel());

    [Authorize(Roles = "Manager,Admin"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult CreateClient(ClientFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var password = RandomNumberGenerator.GetString("abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#", 12);
        var result = accounts.CreateClient(model, password);
        if (!result.Success || result.User is null) { ModelState.AddModelError(nameof(model.Email), result.Error ?? "Не удалось создать клиента."); return View(model); }
        TempData["TemporaryPassword"] = password;
        TempData["NewClientId"] = result.User.Id.ToString();
        TempData["PasswordAction"] = "Новый временный пароль клиента";
        TempData["Success"] = "Аккаунт клиента создан. Скопируйте временный пароль и передайте его клиенту.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Manager,Admin"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Renew(Guid id, string fullName, string phone, string plan, DateTime startDate)
    {
        var result = accounts.UpdateMembership(id, fullName, phone, plan, startDate, true);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Абонемент обновлён." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/Fitness/ResetPassword"), Authorize(Roles = "Manager,Admin"), ValidateAntiForgeryToken]
    public IActionResult ResetPassword(Guid id)
    {
        var password = RandomNumberGenerator.GetString("abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#", 12);
        var result = accounts.ResetClientPassword(id, password);
        if (!result.Success || result.User is null)
        {
            TempData["Error"] = result.Error ?? "Не удалось выдать новый пароль.";
            return RedirectToAction(nameof(Index));
        }

        TempData["TemporaryPassword"] = password;
        TempData["NewClientId"] = result.User.Id.ToString();
        TempData["PasswordAction"] = "Новый временный пароль клиента";
        TempData["Success"] = "Пароль обновлён. Скопируйте его и передайте клиенту.";
        return RedirectToAction(nameof(Index));
    }
}
