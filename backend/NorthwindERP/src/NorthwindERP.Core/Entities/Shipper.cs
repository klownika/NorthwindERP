namespace NorthwindERP.Core.Entities;

// Una fila de la tabla "shippers": empresas que transportan los pedidos.
public class Shipper
{
    public int ShipperId { get; set; }
    public string ShipperName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
