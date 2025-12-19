using BiddingService.Core.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BiddingService.Infrastructure.Encryption;

public class EncryptionValueConverter : ValueConverter<string, string>
{
    public EncryptionValueConverter(IEncryptionService encryptionService)
        : base(
            v => encryptionService.Encrypt(v),
            v => encryptionService.Decrypt(v))
    {
    }
}