using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;

namespace TeamLeave.Domain.Tests;

[Trait("Rule", "RB-10")]
[Trait("Rule", "RB-11")]
[Trait("Rule", "RB-12")]
[Trait("Rule", "RB-25")]
public sealed class BalanceTests
{
    private static readonly Guid Hr = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    private static Balance BalanceOf(int days)
    {
        var balance = Balance.CreateEmpty(Guid.NewGuid(), Guid.NewGuid(), LeaveType.CpId);
        if (days > 0)
        {
            balance.ApplyHrMovement(MovementKind.Allocation, DayAmount.FromDays(days), "Droits", 0, Hr, Now);
        }

        return balance;
    }

    [Fact]
    public void CA_02_1_available_is_amount_minus_pending()
    {
        var balance = BalanceOf(25);

        Assert.Equal(2500, balance.AmountHundredths);
        Assert.Equal(2200, balance.AvailableHundredths(DayAmount.FromDays(3)));
    }

    [Fact]
    public void CA_16_1_allocation_traces_movement_with_balance_after()
    {
        var balance = BalanceOf(0);

        var movement = balance.ApplyHrMovement(MovementKind.Allocation, 2500, "Droits 2026", 0, Hr, Now);

        Assert.Equal(2500, balance.AmountHundredths);
        Assert.Equal(MovementKind.Allocation, movement.Kind);
        Assert.Equal(2500, movement.QuantityHundredths);
        Assert.Equal(2500, movement.BalanceAfterHundredths);
        Assert.Equal("Droits 2026", movement.Reason);
        Assert.Equal(Hr, movement.AuthorId);
    }

    [Fact]
    public void CA_16_2_adjustment_making_available_negative_is_refused()
    {
        var balance = BalanceOf(10);

        var ex = Assert.Throws<DomainException>(() =>
            balance.ApplyHrMovement(MovementKind.Adjustment, -800, "Correction", DayAmount.FromDays(4), Hr, Now));

        Assert.Equal(ErrorCodes.BalanceBelowPending, ex.Code);
        Assert.Equal(1000, balance.AmountHundredths);
    }

    [Fact]
    public void Adjustment_down_to_exactly_pending_is_accepted()
    {
        var balance = BalanceOf(10);

        balance.ApplyHrMovement(MovementKind.Adjustment, -600, "Correction", DayAmount.FromDays(4), Hr, Now);

        Assert.Equal(400, balance.AmountHundredths);
    }

    [Fact]
    public void CA_10_1_debit_subtracts_days_and_links_request()
    {
        var balance = BalanceOf(20);
        var requestId = Guid.NewGuid();

        var movement = balance.Debit(5, requestId, Hr, Now);

        Assert.Equal(1500, balance.AmountHundredths);
        Assert.Equal(MovementKind.Debit, movement.Kind);
        Assert.Equal(-500, movement.QuantityHundredths);
        Assert.Equal(1500, movement.BalanceAfterHundredths);
        Assert.Equal(requestId, movement.LeaveRequestId);
    }

    [Fact]
    public void RB_12_debit_beyond_balance_is_refused_and_balance_unchanged()
    {
        var balance = BalanceOf(2);

        var ex = Assert.Throws<DomainException>(() => balance.Debit(3, Guid.NewGuid(), Hr, Now));

        Assert.Equal(ErrorCodes.InsufficientBalance, ex.Code);
        Assert.Equal(200, balance.AmountHundredths);
    }

    [Fact]
    public void CA_06_2_recredit_adds_days_back()
    {
        var balance = BalanceOf(12);

        var movement = balance.Recredit(3, Guid.NewGuid(), Hr, Now);

        Assert.Equal(1500, balance.AmountHundredths);
        Assert.Equal(MovementKind.Recredit, movement.Kind);
        Assert.Equal(300, movement.QuantityHundredths);
    }

    [Theory]
    [InlineData(MovementKind.Allocation, -100)]
    [InlineData(MovementKind.Allocation, 0)]
    [InlineData(MovementKind.Adjustment, 0)]
    [InlineData(MovementKind.Debit, 100)]
    [InlineData(MovementKind.Recredit, 100)]
    public void Invalid_hr_movements_are_validation_errors(MovementKind kind, int quantity)
    {
        var balance = BalanceOf(10);

        var ex = Assert.Throws<ValidationException>(() => balance.ApplyHrMovement(kind, quantity, "Motif", 0, Hr, Now));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Fact]
    public void Hr_movement_without_reason_is_refused()
    {
        var balance = BalanceOf(10);

        Assert.Throws<ValidationException>(() => balance.ApplyHrMovement(MovementKind.Adjustment, 100, "  ", 0, Hr, Now));
    }

    [Theory]
    [InlineData("25", true, 2500)]
    [InlineData("12.5", true, 1250)]
    [InlineData("0.25", true, 25)]
    [InlineData("-1.75", true, -175)]
    [InlineData("1.255", false, 0)]
    public void Day_amount_conversion_accepts_at_most_two_decimals(string input, bool ok, int expected)
    {
        var success = DayAmount.TryFromDecimal(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture), out var hundredths);

        Assert.Equal(ok, success);
        Assert.Equal(expected, hundredths);
    }
}
