using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using practica415.Data;
using practica415.Models;

namespace practica415.Services;

public sealed class EfAccountService(WarehouseDbContext db, IConfiguration configuration) : IAccountService
{
    private readonly PasswordHasher<WarehouseUser> _hasher = new();
    public bool CanBootstrapAdmin() => !db.Users.Any();

    public AccountResult Register(string login, string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        if (db.Users.Any(x => x.Email == email)) return new(false, Error: "Пользователь с такой почтой уже зарегистрирован.");
        var adminEmail = configuration["FitStock:AdminEmail"]?.Trim();
        var managerEmail = configuration["FitStock:ManagerEmail"]?.Trim();
        var role = !string.IsNullOrWhiteSpace(adminEmail) && adminEmail.Equals(email, StringComparison.OrdinalIgnoreCase) || !db.Users.Any() ? "Admin"
            : !string.IsNullOrWhiteSpace(managerEmail) && managerEmail.Equals(email, StringComparison.OrdinalIgnoreCase) ? "Manager" : "User";
        var user = new WarehouseUser { Login = login.Trim(), Email = email, Role = role };
        user.PasswordHash = _hasher.HashPassword(user, password);
        db.Users.Add(user);
        try { db.SaveChanges(); return new(true, user); }
        catch (DbUpdateException) { return new(false, Error: "Не удалось создать аккаунт. Возможно, такая почта уже используется."); }
    }

    public AccountResult RegisterClient(string login, string fullName, string phone, string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        if (db.Users.Any(x => x.Email == email)) return new(false, Error: "Пользователь с такой почтой уже зарегистрирован.");
        var user = new WarehouseUser { Login = login.Trim(), FullName = fullName.Trim(), Phone = phone.Trim(), Email = email, Role = "User" };
        user.PasswordHash = _hasher.HashPassword(user, password);
        db.Users.Add(user);
        try { db.SaveChanges(); return new(true, user); }
        catch (DbUpdateException) { return new(false, Error: "Не удалось создать аккаунт."); }
    }

    public AccountResult Authenticate(string email, string password)
    {
        var user = db.Users.FirstOrDefault(x => x.Email == email.Trim().ToLowerInvariant());
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            return new(false, Error: "Неверная почта или пароль.");
        return new(true, user);
    }

    public WarehouseUser? GetById(Guid id) => db.Users.AsNoTracking().FirstOrDefault(x => x.Id == id);

    public AccountResult UpdateProfile(Guid id, string login, string? avatarPath)
    {
        var user = db.Users.FirstOrDefault(x => x.Id == id);
        if (user is null) return new(false, Error: "Аккаунт не найден.");
        user.Login = login.Trim();
        user.AvatarPath = avatarPath;
        db.SaveChanges();
        return new(true, user);
    }

    public AccountResult ChangePassword(Guid id, string currentPassword, string newPassword)
    {
        var user = db.Users.FirstOrDefault(x => x.Id == id);
        if (user is null) return new(false, Error: "Аккаунт не найден.");
        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
            return new(false, Error: "Текущий пароль указан неверно.");
        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        db.SaveChanges();
        return new(true, user);
    }

    public IReadOnlyList<AdminUserRow> GetUsers() => db.Users.AsNoTracking().OrderBy(x => x.Login)
        .Select(x => new AdminUserRow { Id = x.Id, Login = x.Login, Email = x.Email, Role = x.Role, AvatarPath = x.AvatarPath, CreatedAt = x.CreatedAt, FullName = x.FullName, Phone = x.Phone, MembershipPlan = x.MembershipPlan, MembershipEnd = x.MembershipEnd }).ToList();

    public IReadOnlyList<WarehouseUser> GetClients() => db.Users.AsNoTracking().Where(x => x.Role == "User").OrderBy(x => x.FullName).ToList();

    public ClientMembershipSummary GetClientMembershipSummary()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var summary = db.Users.AsNoTracking()
            .Where(x => x.Role == "User")
            .GroupBy(_ => 1)
            .Select(group => new ClientMembershipSummary(
                group.Count(),
                group.Count(x => x.MembershipEnd >= today),
                group.Count(x => x.MembershipEnd >= today && x.MembershipEnd <= tomorrow),
                group.Count(x => x.MembershipEnd < today)))
            .SingleOrDefault();

        return summary;
    }

    public AccountResult CreateClient(ClientFormViewModel model, string temporaryPassword)
    {
        var email = model.Email.Trim().ToLowerInvariant();
        if (db.Users.Any(x => x.Email == email)) return new(false, Error: "Аккаунт с такой почтой уже существует.");
        var plan = model.Plan switch { "1month" => ("1 месяц", 1), "3months" => ("3 месяца", 3), "6months" => ("6 месяцев", 6), "1year" => ("1 год", 12), _ => ("", 0) };
        if (plan.Item2 == 0) return new(false, Error: "Выберите срок абонемента.");
        var start = DateTime.SpecifyKind(model.StartDate.Date, DateTimeKind.Unspecified);
        var user = new WarehouseUser { Login = model.Login.Trim(), Email = email, Role = "User", FullName = model.FullName.Trim(), Phone = model.Phone.Trim(), MembershipPlan = plan.Item1, MembershipStart = start, MembershipEnd = start.AddMonths(plan.Item2).AddDays(-1) };
        user.PasswordHash = _hasher.HashPassword(user, temporaryPassword);
        db.Users.Add(user);
        try { db.SaveChanges(); return new(true, user); }
        catch (DbUpdateException) { return new(false, Error: "Не удалось создать аккаунт. Проверьте уникальность почты."); }
    }

    public AccountResult UpdateMembership(Guid id, string fullName, string phone, string planCode, DateTime startDate, bool paid)
    {
        var user = db.Users.FirstOrDefault(x => x.Id == id && x.Role == "User");
        if (user is null) return new(false, Error: "Клиент не найден.");
        var plan = planCode switch { "1month" => ("1 месяц", 1), "3months" => ("3 месяца", 3), "6months" => ("6 месяцев", 6), "1year" => ("1 год", 12), _ => ("", 0) };
        if (plan.Item2 == 0) return new(false, Error: "Выберите срок абонемента.");
        var start = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Unspecified);
        user.FullName = fullName.Trim(); user.Phone = phone.Trim(); user.MembershipPlan = plan.Item1;
        user.MembershipStart = start; user.MembershipEnd = start.AddMonths(plan.Item2).AddDays(-1);
        db.SaveChanges(); return new(true, user);
    }

    public AccountResult SetRole(Guid id, string role)
    {
        if (role is not ("User" or "Manager" or "Admin")) return new(false, Error: "Неизвестная роль.");
        var user = db.Users.FirstOrDefault(x => x.Id == id);
        if (user is null) return new(false, Error: "Аккаунт не найден.");
        if (user.Role == "Admin" && role != "Admin" && db.Users.Count(x => x.Role == "Admin") <= 1)
            return new(false, Error: "Нельзя снять роль у последнего администратора.");
        user.Role = role;
        db.SaveChanges();
        return new(true, user);
    }

    public AccountResult ResetClientPassword(Guid id, string newPassword)
    {
        var user = db.Users.FirstOrDefault(x => x.Id == id && x.Role == "User");
        if (user is null) return new(false, Error: "Клиент не найден.");
        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        db.SaveChanges();
        return new(true, user);
    }
}
