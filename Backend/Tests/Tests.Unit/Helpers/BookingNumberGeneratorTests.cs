using Application.Helpers;
using Xunit;

namespace Tests.Unit.Helpers;

public class BookingNumberGeneratorTests
{
    [Fact]
    public void Generate_ReturnsValidFormat()
    {
        // Act
        var bookingNumber = BookingNumberGenerator.Generate();

        // Assert
        Assert.NotNull(bookingNumber);
        Assert.Matches(@"^BK-\d{6}-[A-Z2-9]{5}$", bookingNumber);
    }

    [Fact]
    public void Generate_ReturnsUniqueNumbers()
    {
        // Act
        var numbers = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            numbers.Add(BookingNumberGenerator.Generate());
        }

        // Assert - All 100 should be unique
        Assert.Equal(100, numbers.Count);
    }

    [Fact]
    public void Generate_ContainsCurrentDate()
    {
        // Act
        var bookingNumber = BookingNumberGenerator.Generate();
        var expectedDate = DateTime.UtcNow.ToString("yyMMdd");

        // Assert
        Assert.Contains(expectedDate, bookingNumber);
    }

    [Fact]
    public void Generate_ThreadSafe_GeneratesUniqueNumbers()
    {
        // Arrange
        var numbers = new HashSet<string>();
        var lockObj = new object();

        // Act - Generate numbers from multiple threads
        Parallel.For(0, 100, _ =>
        {
            var number = BookingNumberGenerator.Generate();
            lock (lockObj)
            {
                numbers.Add(number);
            }
        });

        // Assert - Should still be unique
        Assert.Equal(100, numbers.Count);
    }
}
