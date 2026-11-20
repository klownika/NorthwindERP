using System.ComponentModel.DataAnnotations;

namespace NorthwindERP.Core.DTOs;

// Una línea del pedido. No lleva precio: la base de datos usa Products.Price.
public class OrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 10000)]
    public int Quantity { get; set; }
}

// ENTRA en POST /api/orders/with-customer.
// Se envía customerId (cliente existente) O newCustomer (cliente nuevo), nunca ambos.
// El empleado no va aquí: sale del token.
public class CreateOrderWithCustomerRequest : IValidatableObject
{
    public int? CustomerId { get; set; }

    public NewCustomerRequest? NewCustomer { get; set; }

    // Almacén del que sale todo el pedido
    [Range(1, int.MaxValue)]
    public int WarehouseId { get; set; }

    public int? ShipperId { get; set; }

    [Required]
    [MinLength(1)]
    public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();

    // Validación extra que ASP.NET ejecuta junto con las etiquetas de arriba
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        bool hasCustomerId = CustomerId != null;
        bool hasNewCustomer = NewCustomer != null;

        // Error si vienen los dos o si no viene ninguno
        if (hasCustomerId == hasNewCustomer)
        {
            string[] fields = { nameof(CustomerId), nameof(NewCustomer) };
            yield return new ValidationResult("Envía customerId o newCustomer, pero no ambos.", fields);
        }
    }
}

// SALE de POST /api/orders/with-customer
public class CreateOrderResponse
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal OrderTotal { get; set; }
}

// SALE de GET /api/orders (cabecera de un pedido con su total)
public class OrderSummaryDto
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? ShipperName { get; set; }
    public int LineCount { get; set; }
    public decimal OrderTotal { get; set; }
}

// Un producto dentro del detalle de un pedido (una fila de vw_order_details_extended)
public class OrderLineDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

// SALE de GET /api/orders/{id}: la cabecera + sus líneas
public class OrderDetailDto
{
    public OrderSummaryDto Order { get; set; } = new OrderSummaryDto();
    public List<OrderLineDto> Lines { get; set; } = new List<OrderLineDto>();
}

// SALE de GET /api/orders/customer/{customerId} (una fila de get_customer_order_history)
public class CustomerOrderHistoryDto
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string? ShipperName { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
