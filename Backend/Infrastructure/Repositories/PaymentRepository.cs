using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly CinemaDbContext _context;

    public PaymentRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync(ct);
        return payment;
    }
}
