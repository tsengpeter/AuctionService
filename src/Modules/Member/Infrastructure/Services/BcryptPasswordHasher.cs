using Member.Application.Abstractions;
using BC = BCrypt.Net.BCrypt;

namespace Member.Infrastructure.Services;

public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainPassword)
    {
        return BC.HashPassword(plainPassword, WorkFactor);
    }

    public bool Verify(string plainPassword, string hash)
    {
        return BC.Verify(plainPassword, hash);
    }
}
