using TeamLeave.Application.Common;
using Xunit;

namespace TeamLeave.Domain.Tests;

public sealed class PasswordPolicyTests
{
    [Fact]
    public void Twelve_character_password_is_accepted()
    {
        Assert.True(PasswordPolicy.IsValid("CorrectHorse!2026", out _));
    }

    [Fact]
    public void Short_password_is_refused()
    {
        Assert.False(PasswordPolicy.IsValid("Court!2026", out var reason));
        Assert.Contains("12", reason);
    }

    [Fact]
    public void Common_password_is_refused()
    {
        Assert.False(PasswordPolicy.IsValid("password1234", out _));
    }

    [Fact]
    public void Too_long_password_is_refused()
    {
        Assert.False(PasswordPolicy.IsValid(new string('a', 129), out _));
    }
}
