using Domain.Entities;

namespace Tests.Unit.Builders;

public class ReservationBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private Guid _showtimeId = Guid.NewGuid();
    private IReadOnlyList<string> _seatNumbers = new List<string> { "A1", "A2" };
    private decimal _totalAmount = 20.00m;
    private DateTime _expiresAt = DateTime.UtcNow.AddMinutes(5);
    private ReservationStatus _status = ReservationStatus.Pending;
    private DateTime _createdAt = DateTime.UtcNow;

    public ReservationBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ReservationBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public ReservationBuilder WithShowtimeId(Guid showtimeId)
    {
        _showtimeId = showtimeId;
        return this;
    }

    public ReservationBuilder WithSeatNumbers(IReadOnlyList<string> seatNumbers)
    {
        _seatNumbers = seatNumbers;
        return this;
    }

    public ReservationBuilder WithTotalAmount(decimal amount)
    {
        _totalAmount = amount;
        return this;
    }

    public ReservationBuilder WithExpiresAt(DateTime expiresAt)
    {
        _expiresAt = expiresAt;
        return this;
    }

    public ReservationBuilder WithStatus(ReservationStatus status)
    {
        _status = status;
        return this;
    }

    public ReservationBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public ReservationBuilder AsPending()
    {
        _status = ReservationStatus.Pending;
        return this;
    }

    public ReservationBuilder AsExpired()
    {
        _status = ReservationStatus.Expired;
        return this;
    }

    public ReservationBuilder AsCancelled()
    {
        _status = ReservationStatus.Cancelled;
        return this;
    }

    public ReservationBuilder AsConfirmed()
    {
        _status = ReservationStatus.Confirmed;
        return this;
    }

    public Reservation Build()
    {
        return new Reservation
        {
            Id = _id,
            UserId = _userId,
            ShowtimeId = _showtimeId,
            SeatNumbers = _seatNumbers,
            TotalAmount = _totalAmount,
            ExpiresAt = _expiresAt,
            Status = _status,
            CreatedAt = _createdAt
        };
    }
}
