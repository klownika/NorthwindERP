namespace NorthwindERP.Core.DTOs;

// SALE de GET /api/inventory/stock (una fila de la vista vw_stock_status).
// NeedsRestock = hay que reponer. KardexOk = el stock cuadra con entradas - salidas del Kardex.
public class StockDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int MinStockLevel { get; set; }
    public bool NeedsRestock { get; set; }
    public bool KardexOk { get; set; }
}

// SALE de GET /api/inventory/kardex (una fila de InventoryTransactions).
// Si el pedido se eliminó, OrderId llega null pero el número sigue en Notes.
public class KardexEntryDto
{
    public int TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public string? Notes { get; set; }
}

// SALE de GET /api/inventory/warehouses
public class WarehouseDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int ManagerEmployeeId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
}

// SALE de GET /api/products y GET /api/products/{id}
public class ProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Unit { get; set; }
    public decimal Price { get; set; }
}

// SALE de GET /api/categories
public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
