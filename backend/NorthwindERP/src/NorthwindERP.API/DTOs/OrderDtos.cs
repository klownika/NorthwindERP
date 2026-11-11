using System.ComponentModel.DataAnnotations;

namespace NorthwindERP.API.DTOs;

/// <summary>Línea del pedido. Sin precio: se toma de Products.Price.</summary>
public record OrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, 10_000)]
    public int Quantity { get; init; }
}

/// <summary>
/// Pedido con cliente existente (CustomerId) o nuevo (NewCustomer), nunca ambos.
/// El empleado no va aquí: sale del token.
/// </summary>
public record CreateOrderWithCustomerRequest : IValidatableObject
{
    public int? CustomerId { get; init; }

    public NewCustomerRequest? NewCustomer { get; init; }

    /// <summary>Almacén del que sale todo el pedido.</summary>
    [Range(1, int.MaxValue)]
    public int WarehouseId { get; init; }

    public int? ShipperId { get; init; }

    [Required, MinLength(1)]
    public List<OrderItemRequest> Items { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (CustomerId is null == NewCustomer is null)
            yield return new ValidationResult(
                "Envía customerId o newCustomer, pero no ambos.",
                [nameof(CustomerId), nameof(NewCustomer)]);
    }
}

public record CreateOrderResponse(int OrderId, int CustomerId, decimal OrderTotal);

public record OrderSummaryDto(
    int OrderId,
    DateTime OrderDate,
    int CustomerId,
    string CustomerName,
    string EmployeeName,
    string? ShipperName,
    int LineCount,
    decimal OrderTotal);

public record OrderLineDto(
    int ProductId,
    string ProductName,
    string? CategoryName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderDetailDto(OrderSummaryDto Order, IReadOnlyList<OrderLineDto> Lines);
