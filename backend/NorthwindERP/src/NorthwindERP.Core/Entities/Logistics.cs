namespace NorthwindERP.Core.Entities;

// Tablas originales de Northwind. Los nombres de las propiedades son los de las
// columnas; EF las pasa a minúsculas (ShipperName -> shippername).

/// <summary>Tabla shippers: empresas que transportan los pedidos.</summary>
public class Shipper
{
    public int ShipperId { get; set; }
    public string ShipperName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

/// <summary>Tabla suppliers: proveedores de los productos.</summary>
public class Supplier
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
}
