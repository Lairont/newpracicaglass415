using Microsoft.EntityFrameworkCore;
using practica415.Data;
using practica415.Models;

namespace practica415.Services;

/// <summary>PostgreSQL-backed implementation of the warehouse application service.</summary>
public sealed class EfWarehouseService(WarehouseDbContext db) : IWarehouseService
{
    public IReadOnlyList<Product> GetProducts() => db.Products.AsNoTracking().OrderBy(x => x.Name).ToList();

    public IReadOnlyList<ReportProductRow> GetReportProducts() => db.Products.AsNoTracking()
        .OrderBy(x => x.Name)
        .Select(x => new ReportProductRow(
            x.Name, x.Sku, x.Category, x.Quantity, x.Unit, x.MinimumStock, x.UnitPrice))
        .ToList();

    public Product? GetProduct(int id) => db.Products.AsNoTracking().FirstOrDefault(x => x.Id == id);

    public IReadOnlyList<StockMovement> GetMovements(DateTime? from = null, DateTime? to = null)
    {
        var query = db.StockMovements.AsNoTracking().AsQueryable();
        if (from.HasValue)
        {
            var start = AsDatabaseTimestamp(from.Value.Date);
            query = query.Where(x => x.CreatedAt >= start);
        }
        if (to.HasValue)
        {
            var end = AsDatabaseTimestamp(to.Value.Date.AddDays(1));
            query = query.Where(x => x.CreatedAt < end);
        }
        return query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToList();
    }

    public IReadOnlyList<ReportMovementRow> GetReportMovements(DateTime? from = null, DateTime? to = null)
    {
        var query = db.StockMovements.AsNoTracking().AsQueryable();
        if (from.HasValue)
        {
            var start = AsDatabaseTimestamp(from.Value.Date);
            query = query.Where(x => x.CreatedAt >= start);
        }
        if (to.HasValue)
        {
            var end = AsDatabaseTimestamp(to.Value.Date.AddDays(1));
            query = query.Where(x => x.CreatedAt < end);
        }

        return query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(x => new ReportMovementRow(x.CreatedAt, x.Type, x.ProductName, x.Quantity, x.Unit))
            .ToList();
    }

    public IReadOnlyList<WarehouseOrder> GetOrders() => db.Orders.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToList();

    public OperationResult AddProduct(Product product)
    {
        if (db.Products.Any(x => x.Sku.ToLower() == product.Sku.Trim().ToLower())) return OperationResult.Fail("Товар с таким артикулом уже существует.");
        product.Id = 0; product.Sku = product.Sku.Trim().ToUpperInvariant(); product.Name = product.Name.Trim();
        using var transaction = db.Database.BeginTransaction();
        db.Products.Add(product); db.SaveChanges();
        if (product.Quantity > 0) db.StockMovements.Add(NewMovement(product, MovementType.Receipt, product.Quantity, "Начальный остаток"));
        db.SaveChanges(); transaction.Commit();
        return OperationResult.Ok();
    }

    public OperationResult UpdateProduct(Product product)
    {
        var stored = db.Products.FirstOrDefault(x => x.Id == product.Id);
        if (stored is null) return OperationResult.Fail("Товар не найден.");
        if (db.Products.Any(x => x.Id != product.Id && x.Sku.ToLower() == product.Sku.Trim().ToLower())) return OperationResult.Fail("Товар с таким артикулом уже существует.");
        var oldZone = stored.Zone;
        stored.Name = product.Name.Trim(); stored.Sku = product.Sku.Trim().ToUpperInvariant(); stored.Category = product.Category;
        stored.Unit = product.Unit; stored.MinimumStock = product.MinimumStock; stored.UnitPrice = product.UnitPrice;
        stored.Zone = product.Zone; stored.Condition = product.Condition; stored.PurchaseDate = product.PurchaseDate;
        stored.WarrantyUntil = product.WarrantyUntil; stored.Notes = product.Notes;
        if (!oldZone.Equals(stored.Zone, StringComparison.OrdinalIgnoreCase))
            db.StockMovements.Add(NewMovement(stored, MovementType.Relocation, 0, $"Перемещение из зоны {oldZone} в {stored.Zone}"));
        db.SaveChanges(); return OperationResult.Ok();
    }

    public OperationResult DeleteProduct(int id)
    {
        var product = db.Products.FirstOrDefault(x => x.Id == id);
        if (product is null) return OperationResult.Fail("Товар не найден.");
        if (product.Quantity > 0) return OperationResult.Fail("Нельзя удалить товар с остатком. Сначала оформите отгрузку или списание.");
        if (db.Orders.Any(x => x.ProductId == id && x.Status == OrderStatus.Pending)) return OperationResult.Fail("Нельзя удалить товар, пока по нему есть открытый заказ.");
        db.Products.Remove(product); db.SaveChanges(); return OperationResult.Ok();
    }

    public OperationResult RegisterMovement(int productId, MovementType type, int quantity, string note)
    {
        var product = db.Products.FirstOrDefault(x => x.Id == productId);
        if (product is null) return OperationResult.Fail("Товар не найден.");
        if (quantity <= 0) return OperationResult.Fail("Количество должно быть больше нуля.");
        if (type is not (MovementType.Receipt or MovementType.Shipment or MovementType.WriteOff)) return OperationResult.Fail("Выбран неподдерживаемый тип движения.");
        if (type != MovementType.Receipt && quantity > product.Quantity) return OperationResult.Fail($"Недостаточно товара. Доступно: {product.Quantity} {product.Unit}.");
        product.Quantity += type == MovementType.Receipt ? quantity : -quantity;
        db.StockMovements.Add(NewMovement(product, type, quantity, note.Trim())); db.SaveChanges();
        return OperationResult.Ok();
    }

    public OperationResult AdjustInventory(int productId, int actualQuantity)
    {
        var product = db.Products.FirstOrDefault(x => x.Id == productId);
        if (product is null) return OperationResult.Fail("Товар не найден.");
        if (actualQuantity < 0) return OperationResult.Fail("Фактический остаток не может быть отрицательным.");
        var difference = actualQuantity - product.Quantity;
        if (difference == 0) return OperationResult.Ok();
        product.Quantity = actualQuantity;
        db.StockMovements.Add(NewMovement(product, MovementType.InventoryAdjustment, difference, $"Инвентаризация. Корректировка {difference:+#;-#;0}."));
        db.SaveChanges(); return OperationResult.Ok();
    }

    public OperationResult RedistributeZones()
    {
        var zones = new[] { "A-01", "A-02", "B-01", "B-02", "C-01", "C-02" };
        var products = db.Products.OrderByDescending(p => p.Quantity).ThenBy(p => p.Name).ToList();
        var totals = zones.ToDictionary(zone => zone, _ => 0);
        using var transaction = db.Database.BeginTransaction();
        foreach (var product in products)
        {
            var target = totals.OrderBy(x => x.Value).ThenBy(x => x.Key).First().Key;
            if (!product.Zone.Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                var oldZone = product.Zone;
                product.Zone = target;
                db.StockMovements.Add(new StockMovement { ProductId = product.Id, ProductName = product.Name, Sku = product.Sku, Type = MovementType.Relocation, Quantity = 0, Unit = product.Unit, Zone = $"{oldZone} → {target}", Note = "Автоматическое распределение по зонам хранения", CreatedAt = LocalTimestampNow() });
            }
            totals[target] += product.Quantity;
        }
        db.SaveChanges(); transaction.Commit(); return OperationResult.Ok();
    }

    public OperationResult CreateOrder(string customer, int productId, int quantity)
    {
        var product = db.Products.FirstOrDefault(x => x.Id == productId);
        if (product is null) return OperationResult.Fail("Выберите существующий товар.");
        if (string.IsNullOrWhiteSpace(customer)) return OperationResult.Fail("Укажите получателя заказа.");
        if (quantity <= 0) return OperationResult.Fail("Количество должно быть больше нуля.");
        var reserved = db.Orders.Where(x => x.ProductId == productId && x.Status == OrderStatus.Pending).Sum(x => (int?)x.Quantity) ?? 0;
        var available = product.Quantity - reserved;
        if (quantity > available) return OperationResult.Fail($"Доступно для новых заказов: {available} {product.Unit} товара «{product.Name}».");
        var orderNumber = $"ЗК-{DateTime.Now:yyMMdd}-{Guid.NewGuid():N}";
        db.Orders.Add(new WarehouseOrder { Number = orderNumber, Customer = customer.Trim(), ProductId = product.Id, ProductName = product.Name, Sku = product.Sku, Quantity = quantity, UnitPrice = product.UnitPrice, Status = OrderStatus.Pending, CreatedAt = LocalTimestampNow() });
        db.SaveChanges(); return OperationResult.Ok();
    }

    public OperationResult CompleteOrder(int id)
    {
        var order = db.Orders.FirstOrDefault(x => x.Id == id && x.Status == OrderStatus.Pending);
        if (order is null) return OperationResult.Fail("Открытый заказ не найден.");
        var product = db.Products.FirstOrDefault(x => x.Id == order.ProductId);
        if (product is null || product.Quantity < order.Quantity) return OperationResult.Fail("Недостаточно остатка для отгрузки этого заказа.");
        product.Quantity -= order.Quantity; order.Status = OrderStatus.Completed;
        db.StockMovements.Add(NewMovement(product, MovementType.Shipment, order.Quantity, $"Отгрузка по заказу {order.Number}"));
        db.SaveChanges(); return OperationResult.Ok();
    }

    public OperationResult CancelOrder(int id)
    {
        var order = db.Orders.FirstOrDefault(x => x.Id == id && x.Status == OrderStatus.Pending);
        if (order is null) return OperationResult.Fail("Открытый заказ не найден.");
        order.Status = OrderStatus.Cancelled; db.SaveChanges(); return OperationResult.Ok();
    }

    private static StockMovement NewMovement(Product product, MovementType type, int quantity, string note) => new()
    {
        ProductId = product.Id, ProductName = product.Name, Sku = product.Sku, Type = type,
        Quantity = quantity, Unit = product.Unit, Zone = product.Zone, Note = note, CreatedAt = LocalTimestampNow()
    };

    private static DateTime LocalTimestampNow() => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
    private static DateTime AsDatabaseTimestamp(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
}
