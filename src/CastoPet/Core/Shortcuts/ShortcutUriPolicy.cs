namespace CastoPet.Core;

internal static class ShortcutUriPolicy
{
    public static bool TryGetWebUri(string? target, out Uri? uri)
    {
        if (!Uri.TryCreate(target?.Trim(), UriKind.Absolute, out uri) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
               uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryGetSteamGameUri(string? target, out Uri? uri, out string? gameId)
    {
        gameId = null;
        if (!Uri.TryCreate(target?.Trim(), UriKind.Absolute, out uri) ||
            !uri.Scheme.Equals("steam", StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.Equals("rungameid", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        var path = uri.AbsolutePath.Trim('/');
        if (path.Length == 0 || path.Contains('/') ||
            !ulong.TryParse(path, out var numericId) || numericId == 0)
        {
            return false;
        }

        gameId = path;
        return true;
    }
}
