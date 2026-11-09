namespace NorthwindERP.Core.Entities;

public class Employee
{
    public int EmployeeId { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? Photo { get; set; }
    public string? Notes { get; set; }
}
