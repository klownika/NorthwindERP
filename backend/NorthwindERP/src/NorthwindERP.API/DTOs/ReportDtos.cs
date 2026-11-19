namespace NorthwindERP.API.DTOs;

// Mismos nombres que CustomerSales, CountrySales, etc. en api.models.ts del frontend

public record CustomerSalesDto(int CustomerId, string CustomerName, string? Country, int TotalOrders, int TotalUnits, decimal TotalSales);

public record CountrySalesDto(string? Country, int TotalCustomers, int TotalOrders, decimal TotalSales);

public record CustomerYearSalesDto(int CustomerId, string CustomerName, int SalesYear, int TotalOrders, decimal TotalSales, decimal AvgLineTotal);

public record EmployeeYearSalesDto(int EmployeeId, string EmployeeName, int SalesYear, int TotalOrders, decimal TotalSales, decimal AvgOrderValue);
