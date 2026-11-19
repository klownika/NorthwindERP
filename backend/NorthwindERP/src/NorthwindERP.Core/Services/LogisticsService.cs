using NorthwindERP.Core.Entities;
using NorthwindERP.Core.Interfaces;

namespace NorthwindERP.Core.Services;

// Por ahora no hay reglas de negocio en logística, solo se pasa al repositorio.
// Si luego se filtran empleados inactivos o algo parecido, va aquí.
public class LogisticsService : ILogisticsService
{
    private readonly ILogisticsRepository _repository;

    public LogisticsService(ILogisticsRepository repository) => _repository = repository;

    public Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken ct = default)
        => _repository.GetEmployeesAsync(ct);

    public Task<IReadOnlyList<Shipper>> GetShippersAsync(CancellationToken ct = default)
        => _repository.GetShippersAsync(ct);

    public Task<IReadOnlyList<Supplier>> GetSuppliersAsync(CancellationToken ct = default)
        => _repository.GetSuppliersAsync(ct);
}
