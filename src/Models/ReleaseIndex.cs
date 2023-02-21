using System.Text.Json.Serialization;

namespace DotnetSdkVersionManager.Models;

[Serializable]
public class ReleaseIndexes
{
    [JsonPropertyName("releases-index")] public ReleaseIndex[]? Releases { get; set; }
}

[Serializable]
public class ReleaseIndex
{
    [JsonPropertyName("channel-version")] public string ChannelVersion { get; set; } = "";
    [JsonPropertyName("latest-release")] public string? LatestRelease { get; set; }

    [JsonPropertyName("latest-release-date")]
    public DateTimeOffset LatestReleaseDate { get; set; }

    [JsonPropertyName("latest-runtime")] public string? LatestRuntime { get; set; }
    [JsonPropertyName("latest-sdk")] public string LatestSdk { get; set; } = "";
    [JsonPropertyName("release-type")] public string? ReleaseType { get; set; }
    [JsonPropertyName("support-phase")] public string? SupportPhase { get; set; }
    [JsonPropertyName("eol-date")] public DateTimeOffset? EolDate { get; set; }
    [JsonPropertyName("releases.json")] public string? ReleasesJson { get; set; }
}

[Serializable]
public class ReleaseInfo
{
    [JsonPropertyName("channel-version")] public string? ChannelVersion { get; set; }
    [JsonPropertyName("latest-release")] public string? LatestRelease { get; set; }

    [JsonPropertyName("latest-release-date")]
    public DateTimeOffset LatestReleaseDate { get; set; }

    [JsonPropertyName("latest-runtime")] public string? LatestRuntime { get; set; }
    [JsonPropertyName("latest-sdk")] public string? LatestSdk { get; set; }
    [JsonPropertyName("release-type")] public string? ReleaseType { get; set; }
    [JsonPropertyName("support-phase")] public string? SupportPhase { get; set; }
    [JsonPropertyName("eol-date")] public DateTimeOffset EolDate { get; set; }
    [JsonPropertyName("lifecycle-policy")] public Uri? LifecyclePolicy { get; set; }
    [JsonPropertyName("releases")] public Release[] Releases { get; set; } = Array.Empty<Release>();
}

[Serializable]
public class Release
{
    [JsonPropertyName("release-date")] public DateTimeOffset ReleaseDate { get; set; }
    [JsonPropertyName("release-version")] public string? ReleaseVersion { get; set; }
    [JsonPropertyName("security")] public bool Security { get; set; }
    [JsonPropertyName("cve-list")] public CveList[] CveList { get; set; } = Array.Empty<CveList>();
    [JsonPropertyName("release-notes")] public Uri? ReleaseNotes { get; set; }
    [JsonPropertyName("runtime")] public Runtime Runtime { get; set; } = new();
    [JsonPropertyName("sdk")] public SdkInfo Sdk { get; set; } = new();
    [JsonPropertyName("sdks")] public SdkInfo[] Sdks { get; set; } = Array.Empty<SdkInfo>();

    [JsonPropertyName("aspnetcore-runtime")]
    public AspnetcoreRuntime? AspnetcoreRuntime { get; set; }

    [JsonPropertyName("windowsdesktop")] public Windowsdesktop? Windowsdesktop { get; set; }
}

[Serializable]
public class AspnetcoreRuntime
{
    [JsonPropertyName("version")] public string? Version { get; set; }
    [JsonPropertyName("version-display")] public string? VersionDisplay { get; set; }

    [JsonPropertyName("version-aspnetcoremodule")]
    public string?[] VersionAspnetcoremodule { get; set; } = Array.Empty<string>();

    [JsonPropertyName("vs-version")] public string? VsVersion { get; set; }
    [JsonPropertyName("files")] public SdkFileInfo[] Files { get; set; } = Array.Empty<SdkFileInfo>();
}

[Serializable]
public class SdkFileInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("rid")] public string Rid { get; set; } = "";
    [JsonPropertyName("url")] public Uri? Url { get; set; }
    [JsonPropertyName("hash")] public string? Hash { get; set; }
    [JsonPropertyName("akams")] public Uri? Akams { get; set; }
}

[Serializable]
public class CveList
{
    [JsonPropertyName("cve-id")] public string? CveId { get; set; }
    [JsonPropertyName("cve-url")] public Uri? CveUrl { get; set; }
}

[Serializable]
public class Runtime
{
    [JsonPropertyName("version")] public string? Version { get; set; }
    [JsonPropertyName("version-display")] public string? VersionDisplay { get; set; }
    [JsonPropertyName("vs-version")] public string? VsVersion { get; set; }
    [JsonPropertyName("vs-mac-version")] public string? VsMacVersion { get; set; }
    [JsonPropertyName("files")] public SdkFileInfo[] Files { get; set; } = Array.Empty<SdkFileInfo>();
}

[Serializable]
public class SdkInfo
{
    [JsonPropertyName("version")] public string? Version { get; set; }
    [JsonPropertyName("version-display")] public string? VersionDisplay { get; set; }
    [JsonPropertyName("runtime-version")] public string? RuntimeVersion { get; set; }
    [JsonPropertyName("vs-version")] public string? VsVersion { get; set; }
    [JsonPropertyName("vs-mac-version")] public string? VsMacVersion { get; set; }
    [JsonPropertyName("vs-support")] public string? VsSupport { get; set; }
    [JsonPropertyName("vs-mac-support")] public string? VsMacSupport { get; set; }
    [JsonPropertyName("csharp-version")] public string? CsharpVersion { get; set; }
    [JsonPropertyName("fsharp-version")] public string? FsharpVersion { get; set; }
    [JsonPropertyName("vb-version")] public string? VbVersion { get; set; }
    [JsonPropertyName("files")] public SdkFileInfo[] Files { get; set; } = Array.Empty<SdkFileInfo>();
}

[Serializable]
public class Windowsdesktop
{
    [JsonPropertyName("version")] public string? Version { get; set; }
    [JsonPropertyName("version-display")] public string? VersionDisplay { get; set; }
    [JsonPropertyName("files")] public SdkFileInfo[] Files { get; set; } = Array.Empty<SdkFileInfo>();
}