using Microsoft.EntityFrameworkCore;
using NorthwindERP.Core.DTOs;
using NorthwindERP.Core.Entities;
using NorthwindERP.Core.Exceptions;
using NorthwindERP.Core.Interfaces;
using NorthwindERP.Infrastructure.Data;

namespace NorthwindERP.Infrastructure.Services;

// CRUD de clientes con EF Core sobre la tabla customers
public class CustomerService : ICustomerService
{
    private readonly NorthwindDbContext _db;

    public CustomerService(NorthwindDbContext db)
    {
        _db = db;
    }

    public async Task<List<CustomerDto>> GetAllAsync(CancellationToken ct)
    {
        // 1. Traer todos los clientes ordenados por nombre (AsNoTracking = solo lectura)
        List<Customer> customers = await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.CustomerName)
            .ToListAsync(ct);

        // 2. Convertir cada entidad en DTO
        List<CustomerDto> result = new List<CustomerDto>();
        foreach (Customer customer in customers)
        {
            result.Add(ToDto(customer));
        }

        return result;
    }

    public async Task<CustomerDto> GetByIdAsync(int id, CancellationToken ct)
    {
        Customer customer = await FindCustomerAsync(id, ct);
        return ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(NewCustomerRequest request, CancellationToken ct)
    {
        // 1. El DTO ya exige el nombre, pero un nombre con puros espacios pasaría igual
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            throw new BusinessRuleException("El nombre del cliente es obligatorio.");
        }

        // 2. Pasar los datos del request a una entidad nueva (sin espacios sobrantes)
        Customer customer = new Customer();
        customer.CustomerName = request.CustomerName.Trim();
        customer.ContactName = request.ContactName?.Trim();
        customer.Address = request.Address?.Trim();
        customer.City = request.City?.Trim();
        customer.PostalCode = request.PostalCode?.Trim();
        customer.Country = request.Country?.Trim();

        // 3. Guardar. En SaveChangesAsync se manda el INSERT y PostgreSQL
        //    genera el CustomerID, que EF deja cargado en customer.CustomerId
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateContactAsync(int id, UpdateCustomerContactRequest request, CancellationToken ct)
    {
        // 1. Tiene que venir al menos un campo con valor
        bool nothingToUpdate =
            string.IsNullOrWhiteSpace(request.Address) &&
            string.IsNullOrWhiteSpace(request.City) &&
            string.IsNullOrWhiteSpace(request.PostalCode) &&
            string.IsNullOrWhiteSpace(request.Country);

        if (nothingToUpdate)
        {
            throw new BusinessRuleException("Envía al menos un campo de dirección para actualizar.");
        }

        // 2. Buscar el cliente (404 si no existe)
        Customer customer = await FindCustomerAsync(id, ct);

        // 3. Cambiar solo los campos que llegaron
        if (!string.IsNullOrWhiteSpace(request.Address))
        {
            customer.Address = request.Address.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            customer.City = request.City.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.PostalCode))
        {
            customer.PostalCode = request.PostalCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            customer.Country = request.Country.Trim();
        }

        // 4. EF detecta qué cambió y genera el UPDATE
        await _db.SaveChangesAsync(ct);

        return ToDto(customer);
    }

    // Busca un cliente CON tracking (para poder modificarlo). Lanza 404 si no existe.
    private async Task<Customer> FindCustomerAsync(int id, CancellationToken ct)
    {
        Customer? customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == id, ct);

        if (customer == null)
        {
            throw new NotFoundException($"No existe el cliente {id}.");
        }

        return customer;
    }

    // Copia los datos de la entidad al DTO que sale en el JSON
    private static CustomerDto ToDto(Customer customer)
    {
        CustomerDto dto = new CustomerDto();
        dto.CustomerId = customer.CustomerId;
        dto.CustomerName = customer.CustomerName;
        dto.ContactName = customer.ContactName;
        dto.Address = customer.Address;
        dto.City = customer.City;
        dto.PostalCode = customer.PostalCode;
        dto.Country = customer.Country;

        return dto;
    }
}
