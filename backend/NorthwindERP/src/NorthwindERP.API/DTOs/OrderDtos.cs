namespace NorthwindERP.API.DTOs;

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
