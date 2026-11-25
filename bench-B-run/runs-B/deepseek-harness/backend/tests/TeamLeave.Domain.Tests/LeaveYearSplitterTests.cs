using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;
using TeamLeave.Domain.ValueObjects;
using Xunit;
using static TeamLeave.Domain.Tests.TestData;

namespace TeamLeave.Domain.Tests;

public sealed class LeaveYearSplitterTests
{
    [Fact]
    public void Cross_year_request_is_split_between_the_two_years()
    {
        var slots = new List<LeaveHalfDay>
        {
            new(Date(2026, 12, 31), HalfDay.AM),
            new(Date(2026, 12, 31), HalfDay.PM),
            new(Date(2027, 1, 1), HalfDay.AM),
            new(Date(2027, 1, 1), HalfDay.PM)
        };

        var result = LeaveYearSplitter.Split(slots);

        Assert.Equal(2, result[2026]);
        Assert.Equal(2, result[2027]);
        Assert.Equal(slots.Count, result.Values.Sum());
    }

    [Fact]
    public void Single_year_request_has_one_allocation()
    {
        var slots = Days(Date(2026, 11, 23), Date(2026, 11, 24));

        var result = LeaveYearSplitter.Split(slots);

        Assert.Single(result);
        Assert.Equal(4, result[2026]);
    }
}
