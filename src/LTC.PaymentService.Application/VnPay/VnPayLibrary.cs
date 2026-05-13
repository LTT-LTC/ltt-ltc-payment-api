using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace LTC.PaymentService.VnPay;

/// <summary>
/// VNPAY v2 signing aligned with official samples: sorted <c>vnp_*</c> keys (ordinal),
/// HMAC-SHA512 sign payload uses <c>UrlEncode(key)=UrlEncode(value)</c> pairs joined with <c>&amp;</c>, trailing <c>&amp;</c> stripped before hash.
/// </summary>
public static class VnPayLibrary
{
    private static readonly HashSet<string> HashExcludedKeys =
    [
        "vnp_SecureHash",
        "vnp_SecureHashType"
    ];

    /// <summary>
    /// Builds the full payment redirect URL (sandbox/production vpcpay.html) including <c>vnp_SecureHash</c>.
    /// </summary>
    public static string BuildPaymentRedirectUrl(
        string paymentUrl,
        IDictionary<string, string> requestParametersWithoutHash,
        string hashSecret)
    {
        ArgumentNullException.ThrowIfNull(requestParametersWithoutHash);

        var signData = BuildSignData(requestParametersWithoutHash);
        var secureHash = HmacSha512Hex(hashSecret, signData);

        var sb = new StringBuilder();
        foreach (var kv in OrderVnpParameters(requestParametersWithoutHash))
        {
            sb.Append(WebUtility.UrlEncode(kv.Key))
                .Append('=')
                .Append(WebUtility.UrlEncode(kv.Value))
                .Append('&');
        }

        sb.Append(WebUtility.UrlEncode("vnp_SecureHash"))
            .Append('=')
            .Append(WebUtility.UrlEncode(secureHash));

        return $"{paymentUrl.TrimEnd('/')}?{sb}";
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
        var signData = BuildSignData(parameters);
        var computed = HmacSha512Hex(hashSecret, signData);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(received.Trim().ToLowerInvariant()));
    }

    /// <summary>
    /// Canonical sign string for both pay requests and IPN/return responses (matches VNPAY sample <c>GetResponseData</c> / pay query minus hash).
    /// </summary>
    public static string BuildSignData(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        var sb = new StringBuilder();
        foreach (var kv in OrderVnpParameters(parameters))
        {
            if (HashExcludedKeys.Contains(kv.Key))
                continue;

            sb.Append(WebUtility.UrlEncode(kv.Key))
                .Append('=')
                .Append(WebUtility.UrlEncode(kv.Value))
                .Append('&');
        }

        if (sb.Length > 0)
            sb.Remove(sb.Length - 1, 1);

        return sb.ToString();
    }

    private static IEnumerable<KeyValuePair<string, string>> OrderVnpParameters(
        IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return parameters
            .Where(kv => kv.Key.StartsWith("vnp_", StringComparison.Ordinal))
            .Where(kv => !HashExcludedKeys.Contains(kv.Key))
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal);
    }

    public static string HmacSha512Hex(string hashSecret, string signData)
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

    /// <summary>
    /// True if the collection contains at least one <c>vnp_*</c> key (excluding empty key names).
    /// </summary>
    public static bool HasAnyVnpParameter(IReadOnlyDictionary<string, string> parameters)
    {
        foreach (var kv in parameters)
        {
            if (!string.IsNullOrEmpty(kv.Key) && kv.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
