using Domain.Entities;

namespace Tests.Unit.Builders;

public class SeatBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _showtimeId = Guid.NewGuid();
    private string _seatNumber = "A1";
    private string _seatType = "Regular";
    private decimal _price = 10.00m;
    private SeatStatus _status = SeatStatus.Available;
    private Guid? _reservationId = null;
    private DateTime? _reservedUntil = null;
    private byte[] _rowVersion = Array.Empty<byte>();

    public SeatBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public SeatBuilder WithShowtimeId(Guid id)
    {
        _showtimeId = id;
        return this;
    }

    public SeatBuilder WithSeatNumber(string number)
    {
        _seatNumber = number;
        return this;
    }

    public SeatBuilder WithSeatType(string type)
    {
        _seatType = type;
        return this;
    }

    public SeatBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public SeatBuilder WithStatus(SeatStatus status)
    {
        _status = status;
        return this;
    }

    public SeatBuilder AsReserved(Guid reservationId, DateTime until)
    {
        _status = SeatStatus.Reserved;
        _reservationId = reservationId;
        _reservedUntil = until;
        return this;
    }

    public SeatBuilder AsAvailable()
    {
        _status = SeatStatus.Available;
        _reservationId = null;
        _reservedUntil = null;
        return this;
    }

    public SeatBuilder AsBooked()
    {
        _status = SeatStatus.Booked;
        return this;
    }

    public Seat Build()
    {
        return new Seat
        {
            Id = _id,
            ShowtimeId = _showtimeId,
            SeatNumber = _seatNumber,
            SeatType = _seatType,
            Price = _price,
            Status = _status,
            ReservationId = _reservationId,
            ReservedUntil = _reservedUntil,
            RowVersion = _rowVersion
        };
    }
}
