using NorthwindERP.API.DTOs;
using NorthwindERP.Core.Models;

namespace NorthwindERP.API.Mappings;

public static class InventoryMappings
{
    public static ProductDto ToDto(this Product p) =>
        new(p.ProductId, p.ProductName, p.CategoryId, p.CategoryName, p.SupplierId, p.SupplierName, p.Unit, p.Price);

    public static CategoryDto ToDto(this Category c) => new(c.CategoryId, c.CategoryName, c.Description);

    public static WarehouseDto ToDto(this Warehouse w) =>
        new(w.WarehouseId, w.WarehouseName, w.Location, w.ManagerEmployeeId, w.ManagerName);

    // KardexBalance no se expone; al frontend le basta con saber si cuadra (KardexOk)
    public static StockDto ToDto(this StockItem s) =>
        new(s.WarehouseId, s.WarehouseName, s.ProductId, s.ProductName, s.Quantity, s.MinStockLevel, s.NeedsRestock, s.KardexOk);

    public static KardexEntryDto ToDto(this KardexEntry k) =>
        new(k.TransactionId, k.TransactionDate, k.WarehouseId, k.WarehouseName, k.ProductId, k.ProductName,
            k.TransactionType, k.Quantity, k.EmployeeId, k.EmployeeName, k.OrderId, k.Notes);
}
