using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Booking?> GetByBookingNumberAsync(string bookingNumber, CancellationToken ct = default);
    Task<List<Booking>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Booking> CreateAsync(Booking booking, CancellationToken ct = default);
    Task<Booking> UpdateAsync(Booking booking, CancellationToken ct = default);
}
