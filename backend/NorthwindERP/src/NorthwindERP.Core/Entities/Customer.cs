namespace NorthwindERP.Core.Entities;

// Entidad = una fila de la tabla "customers".
// EF Core pasa los nombres a minúsculas: CustomerName -> columna customername.
public class Customer
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}
