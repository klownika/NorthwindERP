using NorthwindERP.API.DTOs;
using NorthwindERP.Core.Entities;

namespace NorthwindERP.API.Mappings;

public static class LogisticsMappings
{
    // La foto del empleado no se manda: pesa mucho y ninguna pantalla la usa
    public static EmployeeDto ToDto(this Employee e) =>
        new(e.EmployeeId, e.FirstName, e.LastName, $"{e.FirstName} {e.LastName}", e.BirthDate, e.Notes);

    public static ShipperDto ToDto(this Shipper s) => new(s.ShipperId, s.ShipperName, s.Phone);

    public static SupplierDto ToDto(this Supplier s) =>
        new(s.SupplierId, s.SupplierName, s.ContactName, s.Address, s.City, s.Country, s.Phone);
}
