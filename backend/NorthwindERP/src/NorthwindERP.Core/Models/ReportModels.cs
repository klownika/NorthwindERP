namespace NorthwindERP.Core.Models;

/// <summary>Una fila de vw_sales_by_customer. Solo salen clientes con al menos un pedido (regla R9).</summary>
public record CustomerSales(int CustomerId, string CustomerName, string? Country, int TotalOrders, int TotalUnits, decimal TotalSales);

/// <summary>Una fila de vw_sales_by_country.</summary>
public record CountrySales(string? Country, int TotalCustomers, int TotalOrders, decimal TotalSales);

/// <summary>Lo que compró un cliente en un año. AvgLineTotal es el promedio por línea de detalle.</summary>
public record CustomerYearSales(int CustomerId, string CustomerName, int SalesYear, int TotalOrders, decimal TotalSales, decimal AvgLineTotal);

/// <summary>Lo que vendió un empleado en un año. AvgOrderValue es el ticket promedio por pedido.</summary>
public record EmployeeYearSales(int EmployeeId, string EmployeeName, int SalesYear, int TotalOrders, decimal TotalSales, decimal AvgOrderValue);
