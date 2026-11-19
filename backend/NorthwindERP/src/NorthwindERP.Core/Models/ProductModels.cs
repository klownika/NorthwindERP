namespace NorthwindERP.Core.Models;

/// <summary>Datos para registrar un producto en el catálogo.</summary>
public record NewProduct(string ProductName, int? CategoryId, int? SupplierId, string? Unit, decimal Price);
