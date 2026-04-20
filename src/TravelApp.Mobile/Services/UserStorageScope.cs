using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TravelApp.Services;

public static class UserStorageScope
{
    private const string GuestScope = "guest";
    private const string RootFolder = "users";

    public static string GetCurrentScopeKey()
    {
        var identity = !string.IsNullOrWhiteSpace(UserProfileService.UserId)
            ? $"uid:{UserProfileService.UserId.Trim().ToLowerInvariant()}"
            : !string.IsNullOrWhiteSpace(UserProfileService.Email)
                ? $"email:{UserProfileService.Email.Trim().ToLowerInvariant()}"
                : GuestScope;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string GetScopedDirectory(string baseDirectory, params string[]? segments)
    {
        var parts = new List<string> { baseDirectory, RootFolder, GetCurrentScopeKey() };
        if (segments is not null)
        {
            parts.AddRange(segments.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        }

        var directory = Path.Combine(parts.ToArray());
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string GetScopedFilePath(string baseDirectory, string fileName, params string[]? segments)
    {
        var directory = GetScopedDirectory(baseDirectory, segments);
        return Path.Combine(directory, fileName);
    }
}
