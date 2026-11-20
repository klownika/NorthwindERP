using System.ComponentModel.DataAnnotations;

namespace NorthwindERP.Core.DTOs;

// SALE de GET /api/customers, GET /api/customers/{id}, POST y PUT
public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}

// Los StringLength copian el tamaño de las columnas de la tabla customers

// ENTRA en PUT /api/customers/{id}. Solo se actualizan los campos que se envían.
public class UpdateCustomerContactRequest
{
    [StringLength(50)]
    public string? Address { get; set; }

    [StringLength(20)]
    public string? City { get; set; }

    [StringLength(10)]
    public string? PostalCode { get; set; }

    [StringLength(15)]
    public string? Country { get; set; }
}

// ENTRA en POST /api/customers y en el "newCustomer" de POST /api/orders/with-customer
public class NewCustomerRequest
{
    [Required]
    [StringLength(50)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? ContactName { get; set; }

    [StringLength(50)]
    public string? Address { get; set; }

    [StringLength(20)]
    public string? City { get; set; }

    [StringLength(10)]
    public string? PostalCode { get; set; }

    [StringLength(15)]
    public string? Country { get; set; }
}
