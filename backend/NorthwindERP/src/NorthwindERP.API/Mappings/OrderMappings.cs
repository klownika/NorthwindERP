using NorthwindERP.API.DTOs;
using NorthwindERP.Core.Models;

namespace NorthwindERP.API.Mappings;

public static class OrderMappings
{
    public static OrderSummaryDto ToDto(this OrderSummary o) =>
        new(o.OrderId, o.OrderDate, o.CustomerId, o.CustomerName, o.EmployeeName, o.ShipperName, o.LineCount, o.OrderTotal);

    public static OrderLineDto ToDto(this OrderLine l) =>
        new(l.ProductId, l.ProductName, l.CategoryName, l.Quantity, l.UnitPrice, l.LineTotal);

    public static OrderDetailDto ToDto(this OrderDetail d) =>
        new(d.Header.ToDto(), d.Lines.Select(l => l.ToDto()).ToList());
}
