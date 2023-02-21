using System.Net.Http.Json;
using DotnetSdkVersionManager.Interfaces;
using DotnetSdkVersionManager.Models;

namespace DotnetSdkVersionManager;

public class NetSdkRemote : INetSdkRemote
{
    private const string ReleaseIndexUrl = "https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json";
    private const string LinuxInstallScriptUrl = "https://dot.net/v1/dotnet-install.sh";
    private const string MacOsUninstallerUrl = "https://github.com/dotnet/cli-lab/releases/download/1.6.0/dotnet-core-uninstall.tar.gz";

    private readonly HttpClient _httpClient;
    private readonly ISimpleCache _cache;

    public NetSdkRemote(HttpClient httpClient, ISimpleCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
    }

    public async Task<List<ReleaseIndex>> GetReleaseIndices()
    {
        var releaseIndexes = await _cache.GetOrCreate("ReleaseIndexes",
            () => _httpClient.GetFromJsonAsync<ReleaseIndexes>(ReleaseIndexUrl));
        
        return releaseIndexes?.Releases?
                   .Where(x => !x.ChannelVersion.StartsWith("1.") && !x.ChannelVersion.StartsWith("2."))
                   .ToList()
               ?? new List<ReleaseIndex>();
    }

    public async Task<List<Release>> GetReleases(ReleaseIndex releaseIndex)
    {
        var releases = await _cache.GetOrCreate($"ReleaseIndex{releaseIndex.ChannelVersion}",
            () => _httpClient.GetFromJsonAsync<ReleaseInfo>(releaseIndex.ReleasesJson));
        return releases?.Releases.ToList() ?? new List<Release>();
    }

    public async Task<string> DownloadFile(Uri uri, string name)
    {
        var filePath = Path.Combine(Path.GetTempPath(), name);
        await using var fs = File.OpenWrite(filePath);
        await using var stream = await _httpClient.GetStreamAsync(uri);
        await stream.CopyToAsync(fs);
        // todo verify hash
        return filePath;
    }

    public async Task<string> DownloadLinuxInstaller()
    {
        await Task.Yield();
        throw new NotImplementedException();
    }

    public async Task<string> DownloadMacOsUninstaller()
    {
        return await DownloadFile(new Uri(MacOsUninstallerUrl), "dotnet-core-uninstall.tar.gz");
    }
}