namespace NorthwindERP.Core.Models;

/// <summary>Datos para registrar un producto en el catálogo.</summary>
public record NewProduct(string ProductName, int? CategoryId, int? SupplierId, string? Unit, decimal Price);

/// <summary>Cambios de un producto. Lo que llega null se deja como está.</summary>
public record ProductUpdate(string? ProductName, int? CategoryId, int? SupplierId, string? Unit, decimal? Price);
