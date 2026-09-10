using System.Diagnostics.CodeAnalysis;

namespace WMS.Application.Common;

public static class PhoneHelper
{
    [return: NotNullIfNotNull(nameof(phone))]
    public static string? Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return phone;

        // Remove all non-digit characters
        phone = new string(phone.Where(char.IsDigit).ToArray());

        // If 9 digits — add 998 prefix
        if (phone.Length == 9) phone = "998" + phone;

        // Add + prefix for international format
        if (!phone.StartsWith("+"))
            phone = "+" + phone;

        return phone;
    }
}
