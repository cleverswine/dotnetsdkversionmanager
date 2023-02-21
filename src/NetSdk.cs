using DotnetSdkVersionManager.Interfaces;
using DotnetSdkVersionManager.Models;

namespace DotnetSdkVersionManager;

public class NetSdk
{
    private readonly AppConfig _appConfig;
    private readonly INetSdkLocal _netSdkLocal;
    private readonly INetSdkRemote _netSdkRemote;

    public NetSdk(AppConfig appConfig, INetSdkLocal netSdkLocal, INetSdkRemote netSdkRemote)
    {
        _appConfig = appConfig;
        _netSdkLocal = netSdkLocal;
        _netSdkRemote = netSdkRemote;
    }

    public async Task List()
    {
        Console.WriteLine("== LIST ==");
        
        var localSdks = await _netSdkLocal.List();
        var releaseIndices = await _netSdkRemote.GetReleaseIndices();
        var updates = new List<string>();

        foreach (var localSdk in localSdks)
        {
            Console.WriteLine(localSdk);

            var releaseIndex = releaseIndices.GetReleaseIndex(localSdk.ParseFrameworkVersion());
            if (!localSdks.Contains(releaseIndex.LatestSdk))
            {
                updates.Add($"SDK {releaseIndex.LatestSdk} was released on {releaseIndex.LatestReleaseDate:yyyy-MM-dd}. Run `dvm upgrade net{releaseIndex.ChannelVersion}` to install it.");
            }
        }

        if (updates.Any())
        {
            Console.WriteLine("\nUPDATES AVAILABLE");
        }

        foreach (var update in updates.Distinct())
        {
            Console.WriteLine(update);
        }
    }

    public async Task ListAvailable(string framework)
    {
        Console.WriteLine("== LIST AVAILABLE ==");
        
        var localSdks = await _netSdkLocal.List();
        var releaseIndices = await _netSdkRemote.GetReleaseIndices();

        if (framework != "all")
        {
            releaseIndices = releaseIndices
                .Where(x => x.ChannelVersion == framework.ParseFrameworkVersion())
                .ToList();
        }

        foreach (var f in releaseIndices)
        {
            var releases = await _netSdkRemote.GetReleases(f);
            foreach (var release in releases)
            {
                if (release.Sdk.Version == null) continue;
                Console.WriteLine(localSdks.Contains(release.Sdk.Version)
                    ? $"* {release.Sdk.VersionDisplay}"
                    : $"  {release.Sdk.VersionDisplay}");
            }
        }
    }

    public async Task Install(string version)
    {
        Console.WriteLine("== INSTALL ==");
        
        var dots = version.Count(x => x == '.');
        if (dots > 2) throw new ArgumentException("invalid SDK version format", nameof(version));

        var localSdks = await _netSdkLocal.List();
        var releaseIndices = await _netSdkRemote.GetReleaseIndices();
        var frameworkVersion = version.ParseFrameworkVersion();
        var releaseIndex = releaseIndices.GetReleaseIndex(frameworkVersion);
        var releases = await _netSdkRemote.GetReleases(releaseIndex);

        var sdkVersionToInstall = dots < 2 ? releaseIndex.LatestSdk : version;
        if (localSdks.Contains(sdkVersionToInstall))
        {
            Console.WriteLine($"SDK version {sdkVersionToInstall} is already installed");
            return;
        }

        var releaseToInstall = releases.GetRelease(sdkVersionToInstall);

        if (_appConfig.RuntimeIdentifier.Contains("linux"))
        {
            var installScript = await _netSdkRemote.DownloadLinuxInstaller();
            await _netSdkLocal.Run("chmod", $"+x {installScript}");
            await _netSdkLocal.Run(installScript, $"--version {releaseToInstall.Sdk.Version} -i /usr/share/dotnet", true);
            return;
        }

        if (_appConfig.RuntimeIdentifier.Contains("osx"))
        {
            var sdkFileInfo = releaseToInstall.Sdk.Files.FirstOrDefault(x =>
                x.Rid.Equals(_appConfig.RuntimeIdentifier, StringComparison.OrdinalIgnoreCase) && x.Name.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase));
            if (sdkFileInfo?.Url == null) throw new ArgumentException("SDK installation file not found", nameof(version));
            var pkgFile = await _netSdkRemote.DownloadFile(sdkFileInfo.Url, sdkFileInfo.Name);
            await _netSdkLocal.Run("/usr/sbin/installer", $"-pkg {pkgFile} -target /", true);
            return;
        }

        throw new Exception($"runtime identifier {_appConfig.RuntimeIdentifier} is not supported at this time");
    }

    public async Task Uninstall(string version)
    {
        Console.WriteLine("== UNINSTALL ==");
        
        var dots = version.Count(x => x == '.');
        if (version.Contains(' ') || dots != 2) throw new ArgumentException("invalid SDK version format", nameof(version));

        if (_appConfig.RuntimeIdentifier.Contains("linux"))
        {
            // I don't like this...
            await _netSdkLocal.Run("rm", $"-rf /usr/share/dotnet/sdk/{version}", true);
            await _netSdkLocal.Run("rm", $"-rf /usr/share/dotnet/shared/Microsoft.NETCore.App/{version}", true);
            await _netSdkLocal.Run("rm", $"-rf /usr/share/dotnet/shared/Microsoft.AspNetCore.All/{version}", true);
            await _netSdkLocal.Run("rm", $"-rf /usr/share/dotnet/shared/Microsoft.AspNetCore.App/{version}", true);
            await _netSdkLocal.Run("rm", $"-rf /usr/share/dotnet/host/fxr/{version}", true);
            return;
        }

        if (_appConfig.RuntimeIdentifier.Contains("osx"))
        {
            var uninstallScriptTarGz = await _netSdkRemote.DownloadMacOsUninstaller();
            var d = new FileInfo(uninstallScriptTarGz).DirectoryName;
            await _netSdkLocal.Run("tar", $"-zxf {uninstallScriptTarGz} --directory {d}");
            await _netSdkLocal.Run(uninstallScriptTarGz.Replace(".tar.gz", ""), $"remove --sdk {version} --yes");
            return;
        }

        throw new Exception($"runtime identifier {_appConfig.RuntimeIdentifier} is not supported at this time");
    }

    public async Task Upgrade(string framework)
    {
        Console.WriteLine("== UPGRADE ==");
        
        var frameWorkVersion = framework.ParseFrameworkVersion();
        var localSdks = await _netSdkLocal.List();
        var releaseIndices = await _netSdkRemote.GetReleaseIndices();

        foreach (var localSdk in localSdks)
        {
            var localSdkFramework = localSdk.ParseFrameworkVersion();
            if (!string.IsNullOrWhiteSpace(frameWorkVersion) && localSdkFramework != frameWorkVersion) continue;
            
            var releaseIndex = releaseIndices.GetReleaseIndex(localSdkFramework);
            if (localSdk == releaseIndex.LatestSdk) continue;
            
            Console.WriteLine($"installing {releaseIndex.LatestSdk}...");
            await Install(releaseIndex.LatestSdk);
            Console.WriteLine($"removing {localSdk}...");
            await Uninstall(localSdk);
        }
    }
}