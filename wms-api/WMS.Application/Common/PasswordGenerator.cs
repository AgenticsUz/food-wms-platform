using System.Security.Cryptography;

namespace WMS.Application.Common;

/// <summary>
/// Generates the temporary password an operator reads out over the phone.
///
/// The alphabet leaves out every character people confuse when transcribing: 0/O, 1/l/I,
/// 5/S, 2/Z. A password that is technically strong but gets typed wrong three times is
/// worse than a slightly shorter one that lands first time.
/// </summary>
public static class PasswordGenerator
{
    private const string Alphabet = "ABCDEFGHJKMNPQRTUVWXYabcdefghijkmnpqrtuvwxy34679";
    public const int DefaultLength = 12;
    public const int MinimumManualLength = 8;

    public static string Generate(int length = DefaultLength)
    {
        if (length < 4) length = 4;

        var chars = new char[length];
        // RandomNumberGenerator, not Random: this value protects an account.
        var bytes = RandomNumberGenerator.GetBytes(length * 2);

        var b = 0;
        for (var i = 0; i < length; i++)
            chars[i] = Alphabet[bytes[b++] % Alphabet.Length];

        // Guarantee at least one digit — some customers paste this into systems that insist.
        if (!chars.Any(char.IsDigit))
            chars[bytes[b] % length] = "34679"[bytes[b + 1] % 5];

        return new string(chars);
    }
}
