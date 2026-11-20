namespace NorthwindERP.Core.DTOs;

// SALE de GET /api/reports/sales-by-customer (vista vw_sales_by_customer)
public class CustomerSalesDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Country { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUnits { get; set; }
    public decimal TotalSales { get; set; }
}

// SALE de GET /api/reports/sales-by-country (vista vw_sales_by_country)
public class CountrySalesDto
{
    public string? Country { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSales { get; set; }
}

// SALE de GET /api/reports/sales-by-customer-year
// AvgLineTotal = promedio por línea de detalle
public class CustomerYearSalesDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int SalesYear { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AvgLineTotal { get; set; }
}

// SALE de GET /api/reports/sales-by-employee-year
// AvgOrderValue = ticket promedio por pedido
public class EmployeeYearSalesDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int SalesYear { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AvgOrderValue { get; set; }
}
