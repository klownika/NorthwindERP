namespace NorthwindERP.Core.Models;

/// <summary>Valores que acepta el CHECK de InventoryTransactions.TransactionType.</summary>
public static class TransactionTypes
{
    public const string Inbound = "INBOUND";
    public const string Outbound = "OUTBOUND";
    public static readonly IReadOnlySet<string> All = new HashSet<string> { Inbound, Outbound };
}

public record StockFilter(int? WarehouseId, int? ProductId, bool LowStockOnly);

/// <summary>
/// Una fila de vw_stock_status. KardexBalance es lo que da el Kardex
/// (entradas - salidas) y KardexOk dice si coincide con Quantity.
/// </summary>
public record StockItem(
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductName,
    int Quantity,
    int MinStockLevel,
    bool NeedsRestock,
    int KardexBalance,
    bool KardexOk);

public record KardexFilter(int? ProductId, int? WarehouseId, string? Type, DateTime? From, DateTime? To, int Take);

/// <summary>
/// Un movimiento de InventoryTransactions. OrderId queda null en el stock inicial y
/// cuando el pedido se eliminó (la FK es ON DELETE SET NULL, el movimiento no se borra).
/// </summary>
public record KardexEntry(
    int TransactionId,
    DateTime TransactionDate,
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductName,
    string TransactionType,
    int Quantity,
    int EmployeeId,
    string EmployeeName,
    int? OrderId,
    string? Notes);

public record Warehouse(int WarehouseId, string WarehouseName, string Location, int ManagerEmployeeId, string ManagerName);

public record Product(
    int ProductId,
    string ProductName,
    int? CategoryId,
    string? CategoryName,
    int? SupplierId,
    string? SupplierName,
    string? Unit,
    decimal Price);

public record Category(int CategoryId, string CategoryName, string? Description);
