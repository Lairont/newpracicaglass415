using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using practica415.Data;
using practica415.Models;
using practica415.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;

namespace practica415.Controllers;

[AllowAnonymous]
public sealed class AccountController(IAccountService accounts, IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl, CanBootstrapAdmin = accounts.CanBootstrapAdmin() });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = accounts.Authenticate(model.Email, model.Password);
        if (!result.Success || result.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Не удалось войти."); model.CanBootstrapAdmin = accounts.CanBootstrapAdmin(); return View(model);
        }
        await SignIn(result.User);
        return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : Url.Action("Index", "Fitness")!);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = accounts.CanBootstrapAdmin()
            ? accounts.Register(model.Login, model.Email, model.Password)
            : accounts.RegisterClient(model.Login, model.FullName, model.Phone, model.Email, model.Password);
        if (!result.Success || result.User is null)
        {
            ModelState.AddModelError(nameof(model.Email), result.Error ?? "Не удалось создать аккаунт."); return View(model);
        }
        await SignIn(result.User);
        return RedirectToAction("Index", "Fitness");
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Fitness");
    }

    [Authorize(Roles = "Manager,Admin"), HttpGet]
    public IActionResult Profile()
    {
        var user = CurrentUser();
        if (user is null) return Challenge();
        return View(new ProfileViewModel { Login = user.Login, Email = user.Email, AvatarPath = user.AvatarPath });
    }

    [Authorize(Roles = "Manager,Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
    {
        ModelState.Remove(nameof(model.CurrentPassword));
        ModelState.Remove(nameof(model.NewPassword));
        ModelState.Remove(nameof(model.ConfirmPassword));
        var user = CurrentUser();
        if (user is null) return Challenge();
        if (!ModelState.IsValid) { model.Email = user.Email; model.AvatarPath = user.AvatarPath; return View("Profile", model); }

        var avatarPath = user.AvatarPath;
        if (model.Avatar is { Length: > 0 } file)
        {
            if (file.Length > 2 * 1024 * 1024 || !await IsImage(file))
            {
                ModelState.AddModelError(nameof(model.Avatar), "Выберите PNG, JPG или WebP изображение размером до 2 МБ.");
                model.Email = user.Email; model.AvatarPath = user.AvatarPath;
                return View("Profile", model);
            }
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp")) extension = ".jpg";
            var folder = Path.Combine(environment.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using (var output = System.IO.File.Create(Path.Combine(folder, fileName))) await file.CopyToAsync(output);
            avatarPath = $"/uploads/avatars/{fileName}";
        }

        var result = accounts.UpdateProfile(user.Id, model.Login, avatarPath);
        if (!result.Success || result.User is null) { ModelState.AddModelError(string.Empty, result.Error ?? "Не удалось сохранить профиль."); return View("Profile", model); }
        await SignIn(result.User);
        TempData["Success"] = "Профиль сохранён.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize(Roles = "Manager,Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(PasswordChangeViewModel model)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Проверьте поля нового пароля."; return RedirectToAction(nameof(Profile)); }
        var user = CurrentUser();
        if (user is null) return Challenge();
        var result = accounts.ChangePassword(user.Id, model.CurrentPassword, model.NewPassword);
        if (!result.Success || result.User is null) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Profile)); }
        await SignIn(result.User);
        TempData["Success"] = "Пароль изменён.";
        return RedirectToAction(nameof(Profile));
    }

    private WarehouseUser? CurrentUser()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? accounts.GetById(id) : null;
    }

    private static async Task<bool> IsImage(IFormFile file)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header);
        return read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            || read >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255
            || read >= 12 && header[0] == 82 && header[1] == 73 && header[2] == 70 && header[3] == 70 && header[8] == 87 && header[9] == 69 && header[10] == 66 && header[11] == 80;
    }

    private async Task SignIn(WarehouseUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        if (!string.IsNullOrWhiteSpace(user.AvatarPath)) claims = [.. claims, new Claim("avatar", user.AvatarPath)];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
