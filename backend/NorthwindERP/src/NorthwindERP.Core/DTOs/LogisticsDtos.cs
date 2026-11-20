namespace NorthwindERP.Core.DTOs;

// SALE de GET /api/employees.
// FullName ya viene armado para que el frontend no tenga que juntar nombre y apellido.
// La foto no se envía: pesa mucho y ninguna pantalla la usa.
public class EmployeeDto
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? Notes { get; set; }
}

// SALE de GET /api/shippers
public class ShipperDto
{
    public int ShipperId { get; set; }
    public string ShipperName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

// SALE de GET /api/suppliers
public class SupplierDto
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
}
