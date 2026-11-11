using System.ComponentModel.DataAnnotations;

namespace NorthwindERP.API.DTOs;

public record CustomerDto(
    int CustomerId,
    string CustomerName,
    string? ContactName,
    string? Address,
    string? City,
    string? PostalCode,
    string? Country);

// Los StringLength copian el tamaño de las columnas de customers

/// <summary>Cuerpo de PUT /api/customers/{id}. Solo se actualizan los campos enviados.</summary>
public class UpdateCustomerContactRequest
{
    [StringLength(50)] public string? Address { get; set; }
    [StringLength(20)] public string? City { get; set; }
    [StringLength(10)] public string? PostalCode { get; set; }
    [StringLength(15)] public string? Country { get; set; }
}

/// <summary>Datos del cliente nuevo al crear un pedido (POST /api/orders/with-customer).</summary>
public class NewCustomerRequest
{
    [Required, StringLength(50)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(50)] public string? ContactName { get; set; }
    [StringLength(50)] public string? Address { get; set; }
    [StringLength(20)] public string? City { get; set; }
    [StringLength(10)] public string? PostalCode { get; set; }
    [StringLength(15)] public string? Country { get; set; }
}
