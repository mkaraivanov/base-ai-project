namespace Application.Helpers;

public static class BookingNumberGenerator
{
    private static readonly Random _random = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Generates a unique booking number in format: BK-YYMMDD-XXXXX
    /// Example: BK-240115-A1B2C
    /// </summary>
    public static string Generate()
    {
        lock (_lock)
        {
            var date = DateTime.UtcNow.ToString("yyMMdd");
            var randomPart = GenerateRandomAlphanumeric(5);
            return $"BK-{date}-{randomPart}";
        }
    }

    private static string GenerateRandomAlphanumeric(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude similar-looking characters
        var result = new char[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[_random.Next(chars.Length)];
        }

        return new string(result);
    }
}
