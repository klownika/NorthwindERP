using NorthwindERP.API.DTOs;
using NorthwindERP.Core.Entities;

namespace NorthwindERP.API.Mappings;

public static class CustomerMappings
{
    public static CustomerDto ToDto(this Customer c) =>
        new(c.CustomerId, c.CustomerName, c.ContactName, c.Address, c.City, c.PostalCode, c.Country);
}
