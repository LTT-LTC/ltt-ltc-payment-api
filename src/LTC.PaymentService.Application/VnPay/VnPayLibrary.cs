using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace LTC.PaymentService.VnPay;

/// <summary>
/// VNPAY v2 signing: sorted vnp_* keys, URL-encoded values, HMAC-SHA512 hex (lowercase).
/// </summary>
public static class VnPayLibrary
{
    private static readonly HashSet<string> HashExcludedKeys =
    [
        "vnp_SecureHash",
        "vnp_SecureHashType"
    ];

    /// <summary>
    /// Builds HMAC-SHA512 hex (lowercase) over the canonical sign string for the given parameters.
    /// </summary>
    public static string Sign(IDictionary<string, string> parameters, string hashSecret)
    {
        var signData = BuildSignData(parameters);
        return HmacSha512Hex(hashSecret, signData);
    }

    /// <summary>
    /// Validates <paramref name="parameters"/> using <paramref name="hashSecret"/> against <c>vnp_SecureHash</c>.
    /// </summary>
    public static bool ValidateSignature(
        IDictionary<string, string> parameters,
        string hashSecret,
        out string? secureHashFromRequest)
    {
        secureHashFromRequest = null;
        if (!parameters.TryGetValue("vnp_SecureHash", out var received) || string.IsNullOrEmpty(received))
            return false;

        secureHashFromRequest = received;
        var signData = BuildSignData(parameters.Where(kv => !HashExcludedKeys.Contains(kv.Key)));
        var computed = HmacSha512Hex(hashSecret, signData);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(received.Trim().ToLowerInvariant()));
    }

    public static string BuildSignData(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        var filtered = parameters
            .Where(kv => kv.Key.StartsWith("vnp_", StringComparison.Ordinal))
            .Where(kv => !HashExcludedKeys.Contains(kv.Key))
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal);

        var sb = new StringBuilder();
        foreach (var kv in filtered)
        {
            if (sb.Length > 0)
                sb.Append('&');

            sb.Append(kv.Key)
                .Append('=')
                .Append(WebUtility.UrlEncode(kv.Value));
        }

        return sb.ToString();
    }

    private static string HmacSha512Hex(string hashSecret, string signData)
    {
        var keyBytes = Encoding.UTF8.GetBytes(hashSecret);
        var dataBytes = Encoding.UTF8.GetBytes(signData);
        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Converts VND amount (major units) to VNPAY minor units (×100).
    /// </summary>
    public static long ToVnpAmountMinor(decimal amount)
    {
        var minor = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        if (minor < 1 || minor > long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount out of range for VNPAY.");

        return (long)minor;
    }

    /// <summary>
    /// Parses VNPAY <c>vnp_Amount</c> string into major VND units.
    /// </summary>
    public static bool TryParseVnpAmountMajor(string? vnpAmount, out decimal major)
    {
        major = default;
        if (string.IsNullOrWhiteSpace(vnpAmount))
            return false;

        if (!long.TryParse(vnpAmount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor))
            return false;

        major = minor / 100m;
        return true;
    }
}
