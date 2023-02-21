using DotnetSdkVersionManager.Models;

namespace DotnetSdkVersionManager.Interfaces;

public interface INetSdkRemote
{
    Task<List<ReleaseIndex>> GetReleaseIndices();
    Task<List<Release>> GetReleases(ReleaseIndex releaseIndex);
    Task<string> DownloadFile(Uri uri, string name);
    Task<string> DownloadLinuxInstaller();
    Task<string> DownloadMacOsUninstaller();
}