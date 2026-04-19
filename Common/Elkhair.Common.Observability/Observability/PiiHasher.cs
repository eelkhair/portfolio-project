using System.Security.Cryptography;
using System.Text;

namespace Elkhair.Common.Observability.Observability;

/// <summary>
/// Replaces PII values (emails, phone numbers, first/last/full/user names) with a stable,
/// short SHA-256 hash so the same user can be correlated across logs and traces without
/// exposing identifying content.
/// </summary>
public static class PiiHasher
{
    private const string Prefix = "pii_";
    private const int HashHexLength = 12; // 48 bits — low collision risk for correlation, no reversibility

    /// <summary>
    /// Property/tag names (case-insensitive) whose values should be hashed wherever they appear
    /// as Serilog log properties or OTel activity tags.
    /// </summary>
    /// <remarks>
    /// Matching is done by lowercasing the key and checking either exact equality or a suffix
    /// match (for dotted keys like <c>signup.email</c>, <c>otel.tag.user.email</c>,
    /// <c>profile.user_email</c>). Only specific name fields are scrubbed — bare <c>Name</c>
    /// is intentionally NOT matched so company/group/service names are preserved.
    /// </remarks>
    private static readonly string[] PiiSuffixes =
    {
        "email",
        "user_email",
        "phone",
        "phonenumber",
        "firstname",
        "first_name",
        "lastname",
        "last_name",
        "fullname",
        "full_name",
        "username",
        "user_name"
    };

    public static bool IsPiiKey(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        var normalized = key.ToLowerInvariant();
        foreach (var suffix in PiiSuffixes)
        {
            if (string.Equals(normalized, suffix, StringComparison.Ordinal))
                return true;
            // dotted / nested keys: otel.tag.email, signup.email, user.email
            if (normalized.EndsWith("." + suffix, StringComparison.Ordinal))
                return true;
            // camel/pascal concatenations: AdminEmail, CompanyEmail — key suffix ends with the pii
            // token AND the char before it is a letter boundary (already satisfied since suffix is
            // all lowercase and full key was lowercased)
            if (normalized.Length > suffix.Length && normalized.EndsWith(suffix, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns <c>pii_</c>-prefixed 12-char hex digest of the stringified value, or
    /// <c>null</c>/empty pass-through unchanged. Use for stable cross-log correlation
    /// without leaking the identifying value.
    /// </summary>
    public static string? Hash(object? value)
    {
        if (value is null)
            return null;

        var stringified = value as string ?? value.ToString();
        if (string.IsNullOrEmpty(stringified))
            return stringified;

        // Normalize emails to lowercase so "Foo@X.com" and "foo@x.com" hash identically.
        var input = stringified.Trim().ToLowerInvariant();
        Span<byte> digest = stackalloc byte[32];
        var bytes = Encoding.UTF8.GetBytes(input);
        SHA256.HashData(bytes, digest);

        var sb = new StringBuilder(Prefix.Length + HashHexLength);
        sb.Append(Prefix);
        for (var i = 0; i < HashHexLength / 2; i++)
            sb.Append(digest[i].ToString("x2"));
        return sb.ToString();
    }
}
