using NorthwindERP.Core.DTOs;

namespace NorthwindERP.Core.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync(CancellationToken ct);

    // Lanza NotFoundException (404) si el cliente no existe
    Task<CustomerDto> GetByIdAsync(int id, CancellationToken ct);

    Task<CustomerDto> CreateAsync(NewCustomerRequest request, CancellationToken ct);

    Task<CustomerDto> UpdateContactAsync(int id, UpdateCustomerContactRequest request, CancellationToken ct);
}
