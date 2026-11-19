using NorthwindERP.API.DTOs;
using NorthwindERP.Core.Models;

namespace NorthwindERP.API.Mappings;

public static class ReportMappings
{
    public static CustomerSalesDto ToDto(this CustomerSales s) =>
        new(s.CustomerId, s.CustomerName, s.Country, s.TotalOrders, s.TotalUnits, s.TotalSales);

    public static CountrySalesDto ToDto(this CountrySales s) =>
        new(s.Country, s.TotalCustomers, s.TotalOrders, s.TotalSales);

    public static CustomerYearSalesDto ToDto(this CustomerYearSales s) =>
        new(s.CustomerId, s.CustomerName, s.SalesYear, s.TotalOrders, s.TotalSales, s.AvgLineTotal);

    public static EmployeeYearSalesDto ToDto(this EmployeeYearSales s) =>
        new(s.EmployeeId, s.EmployeeName, s.SalesYear, s.TotalOrders, s.TotalSales, s.AvgOrderValue);
}
