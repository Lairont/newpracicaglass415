using System.ComponentModel.DataAnnotations;
using practica415.Data;

namespace practica415.Models;

public enum MovementType
{
    Receipt,
    Shipment,
    WriteOff,
    InventoryAdjustment,
    Relocation
}

public enum OrderStatus
{
    Pending,
    Completed,
    Cancelled
}

public enum InventoryCondition
{
    [Display(Name = "Новый")]
    New,
    [Display(Name = "Исправен")]
    Good,
    [Display(Name = "Нужно обслуживание")]
    NeedsService
}

public sealed class Product
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Name { get; set; } = "";
    [Required, StringLength(24)] public string Sku { get; set; } = "";
    [Required, StringLength(40)] public string Category { get; set; } = "";
    [Required, StringLength(12)] public string Unit { get; set; } = "шт";
    [Range(0, 1000000)] public int Quantity { get; set; }
    [Range(0, 1000000)] public int MinimumStock { get; set; }
    [Range(typeof(decimal), "0", "100000000")] public decimal UnitPrice { get; set; }
    [Required, StringLength(32)] public string Zone { get; set; } = "A-01";
    public InventoryCondition Condition { get; set; } = InventoryCondition.Good;
    public DateTime PurchaseDate { get; set; } = DateTime.Today;
    public DateTime? WarrantyUntil { get; set; }
    [StringLength(400)] public string? Notes { get; set; }
}

public sealed class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Sku { get; set; } = "";
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = "шт";
    public string Zone { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public sealed class WarehouseOrder
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string Customer { get; set; } = "";
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class DashboardViewModel
{
    public IReadOnlyList<Product> Products { get; init; } = [];
    public IReadOnlyList<StockMovement> RecentMovements { get; init; } = [];
    public IReadOnlyList<Product> LowStockProducts { get; init; } = [];
    public int ProductCount { get; init; }
    public int TotalUnits { get; init; }
    public decimal StockValue { get; init; }
    public int PendingOrders { get; init; }
    public int ReceiptsThisMonth { get; init; }
    public int ShipmentsThisMonth { get; init; }
}

public sealed class ProductFormViewModel
{
    public int? Id { get; set; }
    [Required(ErrorMessage = "Укажите название товара")]
    [StringLength(80)] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Укажите артикул")]
    [StringLength(24)] public string Sku { get; set; } = "";
    [Required(ErrorMessage = "Укажите категорию")]
    public string Category { get; set; } = "Инвентарь";
    [Required] public string Unit { get; set; } = "шт";
    [Range(0, 1000000)] public int Quantity { get; set; }
    [Range(0, 1000000)] public int MinimumStock { get; set; }
    [Range(typeof(decimal), "0", "100000000")] public decimal UnitPrice { get; set; }
    [Required] public string Zone { get; set; } = "A-01";
    public InventoryCondition Condition { get; set; } = InventoryCondition.Good;
    [DataType(DataType.Date)] public DateTime PurchaseDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime? WarrantyUntil { get; set; }
    [StringLength(400)] public string? Notes { get; set; }
}

public sealed class CatalogViewModel
{
    public IReadOnlyList<Product> Products { get; init; } = [];
    public IReadOnlyList<string> Categories { get; init; } = [];
    public string? Search { get; init; }
    public string? Category { get; init; }
    public decimal? PriceFrom { get; init; }
    public decimal? PriceTo { get; init; }
    public string Sort { get; init; } = "name";
}

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Укажите ФИО"), StringLength(120, MinimumLength = 3)]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Укажите номер телефона"), Phone, StringLength(30)]
    public string Phone { get; set; } = "";
    [Required(ErrorMessage = "Укажите имя пользователя"), StringLength(50, MinimumLength = 2)]
    public string Login { get; set; } = "";
    [Required, EmailAddress(ErrorMessage = "Укажите корректную почту"), StringLength(254)]
    public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен содержать не менее 8 символов")]
    [DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    [DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}

public sealed class LoginViewModel
{
    [Required, EmailAddress(ErrorMessage = "Укажите корректную почту")]
    public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public bool CanBootstrapAdmin { get; set; }
}

public sealed class ProfileViewModel
{
    [Required(ErrorMessage = "Введите ник"), StringLength(50, MinimumLength = 2)]
    public string Login { get; set; } = "";
    public string? Email { get; set; }
    public string? AvatarPath { get; set; }
    public IFormFile? Avatar { get; set; }
    [DataType(DataType.Password)] public string? CurrentPassword { get; set; }
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен содержать не менее 8 символов"), DataType(DataType.Password)]
    public string? NewPassword { get; set; }
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают"), DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }
}

public sealed class PasswordChangeViewModel
{
    [Required, DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен содержать не менее 8 символов"), DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают"), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class AdminUserRow
{
    public Guid Id { get; init; }
    public string Login { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "User";
    public string? AvatarPath { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? FullName { get; init; }
    public string? Phone { get; init; }
    public string? MembershipPlan { get; init; }
    public DateTime? MembershipEnd { get; init; }
}

public sealed class ClientFormViewModel
{
    public Guid? Id { get; set; }
    [Required, StringLength(120, MinimumLength = 3)] public string FullName { get; set; } = "";
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(50, MinimumLength = 2)] public string Login { get; set; } = "";
    [Required] public string Plan { get; set; } = "1month";
    [DataType(DataType.Date)] public DateTime StartDate { get; set; } = DateTime.Today;
    public bool Paid { get; set; } = true;
}

public sealed class FitnessDashboardViewModel
{
    public WarehouseUser? Client { get; init; }
    public IReadOnlyList<WarehouseUser> Clients { get; init; } = [];
    public string? TemporaryPassword { get; init; }
}

public sealed class MovementFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Выберите товар")]
    public int ProductId { get; set; }
    public MovementType Type { get; set; } = MovementType.Receipt;
    [Range(1, 1000000, ErrorMessage = "Количество должно быть больше нуля")]
    public int Quantity { get; set; }
    [StringLength(120)] public string Note { get; set; } = "";
    public IReadOnlyList<Product> Products { get; init; } = [];
}

public sealed class OrderFormViewModel
{
    [Required, StringLength(80)] public string Customer { get; set; } = "";
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, 1000000)] public int Quantity { get; set; }
    public IReadOnlyList<Product> Products { get; init; } = [];
}

public sealed class InventoryViewModel
{
    public IReadOnlyList<Product> Products { get; init; } = [];
}

public sealed class ReportsViewModel
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public IReadOnlyList<ReportProductRow> Products { get; init; } = [];
    public IReadOnlyList<ReportMovementRow> Movements { get; init; } = [];
    public decimal StockValue { get; init; }
    public int ClientCount { get; init; }
    public int ActiveMemberships { get; init; }
    public int ExpiringMemberships { get; init; }
    public int DebtorCount { get; init; }
}

public readonly record struct ClientMembershipSummary(
    int ClientCount,
    int ActiveMemberships,
    int ExpiringMemberships,
    int DebtorCount);

public readonly record struct ReportProductRow(
    string Name,
    string Sku,
    string Category,
    int Quantity,
    string Unit,
    int MinimumStock,
    decimal UnitPrice);

public readonly record struct ReportMovementRow(
    DateTime CreatedAt,
    MovementType Type,
    string ProductName,
    int Quantity,
    string Unit);
