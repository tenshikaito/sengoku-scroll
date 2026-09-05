namespace SengokuScroll.WebApi;

/// <summary>Browser-origin defense for a trusted-LAN prototype, not user authentication.</summary>
public static class BrowserOriginPolicy
{
    public static string? NormalizeOrigin(string? origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || uri.UserInfo.Length > 0 || uri.AbsolutePath != "/"
            || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            return null;
        return uri.GetLeftPart(UriPartial.Authority);
    }

    public static bool Allows(HttpRequest request, IReadOnlySet<string> additionalOrigins)
    {
        if (!request.Headers.TryGetValue("Origin", out var origins))
            return !string.Equals(request.Headers["Sec-Fetch-Site"].ToString(), "cross-site",
                StringComparison.OrdinalIgnoreCase);

        if (origins.Count != 1) return false;
        var origin = NormalizeOrigin(origins[0]);
        if (origin is null) return false;
        var ownOrigin = NormalizeOrigin($"{request.Scheme}://{request.Host}");
        return string.Equals(origin, ownOrigin, StringComparison.OrdinalIgnoreCase)
            || additionalOrigins.Contains(origin);
    }
}
