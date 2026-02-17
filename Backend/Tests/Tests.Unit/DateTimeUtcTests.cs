using System.Text.Json;
using Xunit;

namespace Tests.Unit;

public class DateTimeUtcTests
{
    [Fact]
    public void DateTimeOffset_UtcDateTime_ShouldHaveUtcKind()
    {
        // Arrange
        var timeProvider = TimeProvider.System;
        var now = timeProvider.GetUtcNow();
        
        // Act - Using .UtcDateTime (the fix)
        var utcDateTime = now.UtcDateTime;
        
        // Assert
        Assert.Equal(DateTimeKind.Utc, utcDateTime.Kind);
        
        // Verify JSON serialization includes 'Z' suffix
        var json = JsonSerializer.Serialize(utcDateTime);
        Assert.Contains("Z", json);
    }
    
    [Fact]
    public void DateTimeOffset_DateTime_PropertyMayNotHaveUtcKind()
    {
        // Arrange
        var timeProvider = TimeProvider.System;
        var now = timeProvider.GetUtcNow();
        
        // Act - Using .DateTime (the old, problematic way)
        var dateTime = now.DateTime;
        
        // Assert - This may NOT be UTC kind (platform dependent)
        // On some systems it might be Unspecified
        var isUtcOrUnspecified = dateTime.Kind == DateTimeKind.Utc || 
                                  dateTime.Kind == DateTimeKind.Unspecified;
        Assert.True(isUtcOrUnspecified);
        
        // If it's Unspecified, JSON won't include 'Z'
        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            var json = JsonSerializer.Serialize(dateTime);
            Assert.DoesNotContain("Z", json);
        }
    }
    
    [Fact]
    public void AddMinutes_UtcDateTime_ShouldPreserveUtcKind()
    {
        // Arrange
        var timeProvider = TimeProvider.System;
        var now = timeProvider.GetUtcNow();
        
        // Act
        var future = now.AddMinutes(5).UtcDateTime;
        
        // Assert
        Assert.Equal(DateTimeKind.Utc, future.Kind);
        
        // Verify JSON serialization
        var json = JsonSerializer.Serialize(future);
        Assert.Contains("Z", json);
    }
}
