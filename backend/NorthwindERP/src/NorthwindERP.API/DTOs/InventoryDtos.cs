namespace NorthwindERP.API.DTOs;

// Mismos nombres que frontend/src/app/core/models/api.models.ts

/// <summary>
/// Stock de un producto en un almacén. NeedsRestock = hay que reponer;
/// KardexOk = el stock coincide con entradas - salidas del Kardex.
/// </summary>
public record StockDto(
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductName,
    int Quantity,
    int MinStockLevel,
    bool NeedsRestock,
    bool KardexOk);

/// <summary>
/// Movimiento del Kardex. Si el pedido se eliminó, OrderId llega null pero el
/// número sigue en Notes, así el historial no se pierde (regla R7).
/// </summary>
public record KardexEntryDto(
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

public record WarehouseDto(int WarehouseId, string WarehouseName, string Location, int ManagerEmployeeId, string ManagerName);

public record ProductDto(
    int ProductId,
    string ProductName,
    int? CategoryId,
    string? CategoryName,
    int? SupplierId,
    string? SupplierName,
    string? Unit,
    decimal Price);

public record CategoryDto(int CategoryId, string CategoryName, string? Description);
