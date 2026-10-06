namespace Anthropic.Net.Platforms;

using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

/// <summary>AWS credentials used for SigV4 signing.</summary>
/// <param name="AccessKeyId">The access key id.</param>
/// <param name="SecretAccessKey">The secret access key.</param>
/// <param name="SessionToken">The session token for temporary credentials.</param>
public sealed record AwsCredentials(string AccessKeyId, string SecretAccessKey, string? SessionToken = null)
{
    /// <summary>Reads AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY and AWS_SESSION_TOKEN.</summary>
    /// <returns>The credentials.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the key variables are missing.</exception>
    public static AwsCredentials FromEnvironment()
    {
        var id = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secret = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY must be set (or pass AwsCredentials / a credentials provider explicitly).");
        }

        return new AwsCredentials(id, secret, Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN"));
    }
}

/// <summary>AWS Signature Version 4 request signing (no AWS SDK dependency).</summary>
public static class AwsSigV4
{
    /// <summary>Signs a request in place: adds x-amz-date, x-amz-security-token (if any) and Authorization.</summary>
    /// <param name="request">The request (URI must be absolute; content must be buffered).</param>
    /// <param name="credentials">The credentials.</param>
    /// <param name="region">The AWS region.</param>
    /// <param name="service">The signing service name.</param>
    /// <param name="now">Timestamp override (tests).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public static async Task SignAsync(HttpRequestMessage request, AwsCredentials credentials, string region, string service, DateTimeOffset? now = null, CancellationToken cancellationToken = default)
    {
        var time = (now ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var amzDate = time.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var date = time.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var uri = request.RequestUri ?? throw new ArgumentException("The request needs an absolute URI.", nameof(request));

        var payload = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        var payloadHash = Hex(SHA256.HashData(payload));

        request.Headers.Remove("x-amz-date");
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.Remove("x-amz-security-token");
        if (!string.IsNullOrEmpty(credentials.SessionToken))
        {
            request.Headers.TryAddWithoutValidation("x-amz-security-token", credentials.SessionToken);
        }

        var headers = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["host"] = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}",
            ["x-amz-date"] = amzDate,
        };
        if (!string.IsNullOrEmpty(credentials.SessionToken))
        {
            headers["x-amz-security-token"] = credentials.SessionToken;
        }

        var signedHeaders = string.Join(';', headers.Keys);
        var canonicalHeaders = string.Concat(headers.Select(h => $"{h.Key}:{h.Value.Trim()}\n"));
        var canonicalRequest = string.Join(
            '\n',
            request.Method.Method,
            CanonicalPath(uri),
            CanonicalQuery(uri),
            canonicalHeaders,
            signedHeaders,
            payloadHash);

        var scope = $"{date}/{region}/{service}/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{scope}\n{Hex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))}";

        var key = Hmac(Encoding.UTF8.GetBytes("AWS4" + credentials.SecretAccessKey), date);
        key = Hmac(key, region);
        key = Hmac(key, service);
        key = Hmac(key, "aws4_request");
        var signature = Hex(Hmac(key, stringToSign));

        request.Headers.Remove("Authorization");
        request.Headers.TryAddWithoutValidation("Authorization", $"AWS4-HMAC-SHA256 Credential={credentials.AccessKeyId}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}");
    }

    private static string CanonicalPath(Uri uri)
    {
        // Non-S3 services double-encode: percent-encode the already-encoded path segments.
        var path = uri.AbsolutePath;
        return path.Length == 0 ? "/" : string.Join('/', path.Split('/').Select(Encode));
    }

    private static string CanonicalQuery(Uri uri)
    {
        var query = uri.Query.TrimStart('?');
        if (query.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(
            '&',
            query.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Select(p => (Key: Encode(Uri.UnescapeDataString(p[0])), Value: Encode(p.Length > 1 ? Uri.UnescapeDataString(p[1]) : string.Empty)))
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .ThenBy(p => p.Value, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={p.Value}"));
    }

    private static string Encode(string value)
    {
        var sb = new StringBuilder();
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            var c = (char)b;
            if (c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '_' or '.' or '~')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
        }

        return sb.ToString();
    }

    private static byte[] Hmac(byte[] key, string data) => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));

    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
}
