using practica415.Models;

namespace practica415.Services;

public sealed record OperationResult(bool Success, string? Error = null)
{
    public static OperationResult Ok() => new(true);
    public static OperationResult Fail(string error) => new(false, error);
}

public interface IWarehouseService
{
    IReadOnlyList<Product> GetProducts();
    IReadOnlyList<ReportProductRow> GetReportProducts();
    Product? GetProduct(int id);
    IReadOnlyList<StockMovement> GetMovements(DateTime? from = null, DateTime? to = null);
    IReadOnlyList<ReportMovementRow> GetReportMovements(DateTime? from = null, DateTime? to = null);
    IReadOnlyList<WarehouseOrder> GetOrders();
    OperationResult AddProduct(Product product);
    OperationResult UpdateProduct(Product product);
    OperationResult DeleteProduct(int id);
    OperationResult RegisterMovement(int productId, MovementType type, int quantity, string note);
    OperationResult AdjustInventory(int productId, int actualQuantity);
    OperationResult RedistributeZones();
    OperationResult CreateOrder(string customer, int productId, int quantity);
    OperationResult CompleteOrder(int id);
    OperationResult CancelOrder(int id);
}
