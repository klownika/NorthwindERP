using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Entities;

namespace NorthwindERP.API.Mappings;

public static class CustomerMappings
{
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
