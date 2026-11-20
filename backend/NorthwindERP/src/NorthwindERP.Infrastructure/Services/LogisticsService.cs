using Microsoft.EntityFrameworkCore;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Entities;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.Infrastructure.Services;

// Listas de solo lectura (empleados, transportistas, proveedores) con EF Core.
// AsNoTracking porque solo se leen, nunca se modifican desde aquí.
public class LogisticsService : ILogisticsService
{
    private readonly NorthwindDbContext _db;

    public LogisticsService(NorthwindDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmployeeDto>> GetEmployeesAsync(CancellationToken ct)
    {
        // 1. Empleados ordenados por apellido y luego por nombre
        List<Employee> employees = await _db.Employees
            .AsNoTracking()
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync(ct);

        // 2. Convertir a DTO (sin la foto) y armar el nombre completo
        List<EmployeeDto> result = new List<EmployeeDto>();
        foreach (Employee employee in employees)
        {
            EmployeeDto dto = new EmployeeDto();
            dto.EmployeeId = employee.EmployeeId;
            dto.FirstName = employee.FirstName;
            dto.LastName = employee.LastName;
            dto.FullName = employee.FirstName + " " + employee.LastName;
            dto.BirthDate = employee.BirthDate;
            dto.Notes = employee.Notes;

            result.Add(dto);
        }

        return result;
    }

    public async Task<List<ShipperDto>> GetShippersAsync(CancellationToken ct)
    {
        List<Shipper> shippers = await _db.Shippers
            .AsNoTracking()
            .OrderBy(s => s.ShipperName)
            .ToListAsync(ct);

        List<ShipperDto> result = new List<ShipperDto>();
        foreach (Shipper shipper in shippers)
        {
            ShipperDto dto = new ShipperDto();
            dto.ShipperId = shipper.ShipperId;
            dto.ShipperName = shipper.ShipperName;
            dto.Phone = shipper.Phone;

            result.Add(dto);
        }

        return result;
    }

    public async Task<List<SupplierDto>> GetSuppliersAsync(CancellationToken ct)
    {
        List<Supplier> suppliers = await _db.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.SupplierName)
            .ToListAsync(ct);

        List<SupplierDto> result = new List<SupplierDto>();
        foreach (Supplier supplier in suppliers)
        {
            SupplierDto dto = new SupplierDto();
            dto.SupplierId = supplier.SupplierId;
            dto.SupplierName = supplier.SupplierName;
            dto.ContactName = supplier.ContactName;
            dto.Address = supplier.Address;
            dto.City = supplier.City;
            dto.Country = supplier.Country;
            dto.Phone = supplier.Phone;

            result.Add(dto);
        }

        return result;
    }
}
