using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using practica415.Models;
using practica415.Services;

namespace practica415.Controllers;

public sealed class InventoryController(IWarehouseService warehouse) : Controller
{
    public IActionResult Index() => View(new InventoryViewModel { Products = warehouse.GetProducts() });

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult Reconcile(int productId, int actualQuantity)
    {
        var result = warehouse.AdjustInventory(productId, actualQuantity);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Результат инвентаризации сохранён." : result.Error;
        return RedirectToAction(nameof(Index));
    }

}
