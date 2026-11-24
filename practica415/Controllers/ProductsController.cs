using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using practica415.Models;
using practica415.Services;

namespace practica415.Controllers;

public sealed class ProductsController(IWarehouseService warehouse) : Controller
{
    public IActionResult Index(string? search, string? category, decimal? priceFrom, decimal? priceTo, string sort = "name")
    {
        var allProducts = warehouse.GetProducts();
        var products = allProducts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search)) products = products.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || p.Sku.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(category)) products = products.Where(p => p.Category == category);
        if (priceFrom.HasValue) products = products.Where(p => p.UnitPrice >= priceFrom.Value);
        if (priceTo.HasValue) products = products.Where(p => p.UnitPrice <= priceTo.Value);
        products = sort switch
        {
            "price-asc" => products.OrderBy(p => p.UnitPrice),
            "price-desc" => products.OrderByDescending(p => p.UnitPrice),
            "quantity-asc" => products.OrderBy(p => p.Quantity),
            "quantity-desc" => products.OrderByDescending(p => p.Quantity),
            _ => products.OrderBy(p => p.Name)
        };
        return View(new CatalogViewModel
        {
            Products = products.ToList(), Categories = allProducts.Select(p => p.Category).Distinct().Order().ToList(),
            Search = search, Category = category, PriceFrom = priceFrom, PriceTo = priceTo, Sort = sort
        });
    }

    public IActionResult Details(int id)
    {
        var product = warehouse.GetProduct(id);
        return product is null ? NotFound() : View(product);
    }

    [HttpGet]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult Create() => View("Form", new ProductFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);
        var result = warehouse.AddProduct(ToProduct(model));
        if (!result.Success) { ModelState.AddModelError(nameof(model.Sku), result.Error!); return View("Form", model); }
        TempData["Success"] = "Карточка товара добавлена.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult Edit(int id)
    {
        var product = warehouse.GetProduct(id);
        if (product is null) return NotFound();
        return View("Form", new ProductFormViewModel { Id = product.Id, Name = product.Name, Sku = product.Sku, Category = product.Category, Unit = product.Unit, Quantity = product.Quantity, MinimumStock = product.MinimumStock, UnitPrice = product.UnitPrice, Zone = product.Zone, Condition = product.Condition, PurchaseDate = product.PurchaseDate, WarrantyUntil = product.WarrantyUntil, Notes = product.Notes });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult Edit(int id, ProductFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View("Form", model);
        var result = warehouse.UpdateProduct(ToProduct(model));
        if (!result.Success) { ModelState.AddModelError(nameof(model.Sku), result.Error!); return View("Form", model); }
        TempData["Success"] = "Карточка товара обновлена.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id)
    {
        var result = warehouse.DeleteProduct(id);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Товар удалён." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    private static Product ToProduct(ProductFormViewModel model) => new()
    {
        Id = model.Id ?? 0, Name = model.Name.Trim(), Sku = model.Sku.Trim().ToUpperInvariant(), Category = model.Category,
        Unit = model.Unit, Quantity = model.Quantity, MinimumStock = model.MinimumStock, UnitPrice = model.UnitPrice, Zone = model.Zone,
        Condition = model.Condition,
        PurchaseDate = DateTime.SpecifyKind(model.PurchaseDate.Date, DateTimeKind.Unspecified),
        WarrantyUntil = model.WarrantyUntil.HasValue ? DateTime.SpecifyKind(model.WarrantyUntil.Value.Date, DateTimeKind.Unspecified) : null,
        Notes = model.Notes
    };
}
