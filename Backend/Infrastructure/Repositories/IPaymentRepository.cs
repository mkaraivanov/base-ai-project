using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default);
    Task<Payment> UpdateAsync(Payment payment, CancellationToken ct = default);
}
