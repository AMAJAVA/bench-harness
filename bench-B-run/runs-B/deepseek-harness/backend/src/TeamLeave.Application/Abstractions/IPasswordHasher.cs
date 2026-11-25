namespace TeamLeave.Application.Abstractions;

public interface IPasswordHasher
{
    Task<string> HashAsync(string password, CancellationToken cancellationToken = default);
    Task<bool> VerifyAsync(string password, string hash, CancellationToken cancellationToken = default);
}
