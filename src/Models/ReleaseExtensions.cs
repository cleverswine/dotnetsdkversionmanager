namespace DotnetSdkVersionManager.Models;

public static class ReleaseExtensions
{
    public static string ParseFrameworkVersion(this string version)
    {
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentNullException(nameof(version));
        var parts = version
            .Replace("net", "")
            .Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 ? $"{version}.0" : $"{parts[0]}.{parts[1]}";
    }

    public static ReleaseIndex GetReleaseIndex(this IEnumerable<ReleaseIndex> releaseIndexes, string frameworkVersion)
    {
        var releaseIndex = releaseIndexes.FirstOrDefault(x => x.ChannelVersion == frameworkVersion);
        if (releaseIndex == null) throw new Exception($"no release channel found for {frameworkVersion}");
        return releaseIndex;
    }

    public static Release GetRelease(this IEnumerable<Release> releases, string sdkVersion)
    {
        var release = releases.FirstOrDefault(x => x.Sdk.Version == sdkVersion);
        if (release == null) throw new Exception($"no release found for SDK {sdkVersion}");
        return release;
    }
}