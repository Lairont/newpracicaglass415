using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using practica415.Models;
using practica415.Services;
using System.Text;

namespace practica415.Controllers;

[Authorize(Roles = "Manager,Admin")]
public sealed class ReportsController(IWarehouseService warehouse, IAccountService accounts) : Controller
{
    public IActionResult Index(DateTime? from, DateTime? to)
    {
        var start = from?.Date ?? DateTime.Today.AddDays(-29);
        var end = to?.Date ?? DateTime.Today;
        if (start > end) (start, end) = (end, start);
        start = DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
        end = DateTime.SpecifyKind(end, DateTimeKind.Unspecified);
        var movements = warehouse.GetReportMovements(start, end);
        var products = warehouse.GetReportProducts();
        var clients = accounts.GetClientMembershipSummary();
        return View(new ReportsViewModel
        {
            From = start, To = end, Products = products, Movements = movements,
            ClientCount = clients.ClientCount,
            ActiveMemberships = clients.ActiveMemberships,
            ExpiringMemberships = clients.ExpiringMemberships,
            DebtorCount = clients.DebtorCount,
            StockValue = products.Sum(p => p.Quantity * p.UnitPrice)
        });
    }

    public IActionResult Export(DateTime? from, DateTime? to)
    {
        var start = DateTime.SpecifyKind((from ?? DateTime.Today.AddDays(-29)).Date, DateTimeKind.Unspecified);
        var end = DateTime.SpecifyKind((to ?? DateTime.Today).Date, DateTimeKind.Unspecified);
        var movements = warehouse.GetMovements(start, end);
        var rows = new List<string> { "Дата;Товар;Артикул;Операция;Количество;Единица;Зона;Комментарий" };
        rows.AddRange(movements.Select(m => string.Join(';', new[] { m.CreatedAt.ToString("yyyy-MM-dd HH:mm"), m.ProductName, m.Sku, MovementLabel(m.Type), m.Quantity.ToString(), m.Unit, m.Zone, m.Note }.Select(EscapeCsv))));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(string.Join("\r\n", rows))).ToArray(), "text/csv; charset=utf-8", "fitness-store-report.csv");
    }

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static string MovementLabel(MovementType type) => type switch
    {
        MovementType.Receipt => "Поступление", MovementType.Shipment => "Отгрузка", MovementType.WriteOff => "Списание",
        MovementType.InventoryAdjustment => "Инвентаризация", MovementType.Relocation => "Перемещение", _ => type.ToString()
    };
}
