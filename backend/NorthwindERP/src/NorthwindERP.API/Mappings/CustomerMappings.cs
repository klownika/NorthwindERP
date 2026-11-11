using NorthwindERP.API.DTOs;
using NorthwindERP.Core.Entities;

namespace NorthwindERP.API.Mappings;

public static class CustomerMappings
{
    public static CustomerDto ToDto(this Customer c) =>
        new(c.CustomerId, c.CustomerName, c.ContactName, c.Address, c.City, c.PostalCode, c.Country);

    public static Customer ToEntity(this NewCustomerRequest r) => new()
    {
        CustomerName = r.CustomerName.Trim(),
        ContactName = r.ContactName?.Trim(),
        Address = r.Address?.Trim(),
        City = r.City?.Trim(),
        PostalCode = r.PostalCode?.Trim(),
        Country = r.Country?.Trim()
    };
}
