using Microsoft.Extensions.Options;
using TeamLeave.Infrastructure.Identity;
using Xunit;

namespace TeamLeave.Domain.Tests;

public sealed class Pbkdf2PasswordHasherTests
{
    private static Pbkdf2PasswordHasher Create() =>
        new(Options.Create(new PasswordHashingOptions { Iterations = 100_000 }));

    [Fact]
    public async Task Password_can_be_verified()
    {
        var hasher = Create();
        var hash = await hasher.HashAsync("CorrectHorse!2026", TestContext.Current.CancellationToken);

        Assert.True(await hasher.VerifyAsync("CorrectHorse!2026", hash, TestContext.Current.CancellationToken));
        Assert.False(await hasher.VerifyAsync("WrongPassword!2026", hash, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Salts_are_unique_for_the_same_password()
    {
        var hasher = Create();
        var first = await hasher.HashAsync("CorrectHorse!2026", TestContext.Current.CancellationToken);
        var second = await hasher.HashAsync("CorrectHorse!2026", TestContext.Current.CancellationToken);

        Assert.NotEqual(first, second);
    }
}
