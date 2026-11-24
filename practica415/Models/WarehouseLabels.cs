namespace practica415.Models;

public static class WarehouseLabels
{
    public static string Movement(MovementType type) => type switch
    {
        MovementType.Receipt => "Поступление",
        MovementType.Shipment => "Отгрузка",
        MovementType.WriteOff => "Списание",
        MovementType.InventoryAdjustment => "Инвентаризация",
        MovementType.Relocation => "Перемещение",
        _ => "Операция"
    };

    public static string Order(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Ожидает отгрузки",
        OrderStatus.Completed => "Отгружен",
        OrderStatus.Cancelled => "Отменён",
        _ => "Новый"
    };
}

public static class InventoryConditionLabels
{
    public static string Display(InventoryCondition condition) => condition switch
    {
        InventoryCondition.New => "Новый",
        InventoryCondition.Good => "Исправен",
        InventoryCondition.NeedsService => "Нужно обслуживание",
        _ => "Не указано"
    };
}
