using practica415.Data;
using practica415.Models;

namespace practica415.Services;

public sealed record AccountResult(bool Success, WarehouseUser? User = null, string? Error = null);

public interface IAccountService
{
    AccountResult Register(string login, string email, string password);
    AccountResult RegisterClient(string login, string fullName, string phone, string email, string password);
    AccountResult Authenticate(string email, string password);
    WarehouseUser? GetById(Guid id);
    AccountResult UpdateProfile(Guid id, string login, string? avatarPath);
    AccountResult ChangePassword(Guid id, string currentPassword, string newPassword);
    IReadOnlyList<AdminUserRow> GetUsers();
    AccountResult SetRole(Guid id, string role);
    bool CanBootstrapAdmin();
    IReadOnlyList<WarehouseUser> GetClients();
    ClientMembershipSummary GetClientMembershipSummary();
    AccountResult CreateClient(ClientFormViewModel model, string temporaryPassword);
    AccountResult UpdateMembership(Guid id, string fullName, string phone, string plan, DateTime startDate, bool paid);
    AccountResult ResetClientPassword(Guid id, string newPassword);
}
