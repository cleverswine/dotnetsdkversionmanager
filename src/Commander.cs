using DotnetSdkVersionManager.Interfaces;
using DotnetSdkVersionManager.Models;

namespace DotnetSdkVersionManager;

public class Commander
{
    private readonly AppConfig _appConfig;
    private readonly INetSdkLocal _netSdkLocal;
    private readonly INetSdkRemote _netSdkRemote;
    
    // TODO - work on better output + logging
    private static void WriteTitle(string s) => Console.WriteLine($"--------------\n{s}\n--------------");
    private static void WriteStep(string s) => Console.WriteLine($"=> {s}");
    private static void WriteLine(string s) => Console.WriteLine(s);
    
    public Commander(AppConfig appConfig, INetSdkLocal netSdkLocal, INetSdkRemote netSdkRemote)
    {
        _appConfig = appConfig;
        _netSdkLocal = netSdkLocal;
        _netSdkRemote = netSdkRemote;
    }

    public async Task List()
    {
        WriteTitle("Installed SDKs");

        var localSdks = await _netSdkLocal.List();
        var releaseIndices = await _netSdkRemote.GetReleaseIndices();
        var updates = new List<string>();

        foreach (var localSdk in localSdks)
        {
            WriteLine("* " + localSdk);

            var releaseIndex = releaseIndices.GetReleaseIndex(localSdk.ParseFrameworkVersion());
            if (!localSdks.Contains(releaseIndex.LatestSdk))
            {
                updates.Add(
                    $"SDK {releaseIndex.LatestSdk} was released on {releaseIndex.LatestReleaseDate:yyyy-MM-dd}. Use the upgrade command to install it.");
            }
        }

        WriteLine(updates.Any() ? "\nUPDATES AVAILABLE" : "\nEverything is up to date!");

        foreach (var update in updates.Distinct())
        {
            WriteLine(update);
        }
    }

    public async Task ListAvailable(string framework)
    {
        WriteTitle("Available SDKs");

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
                WriteLine(localSdks.Contains(release.Sdk.Version)
                    ? $"* {release.Sdk.VersionDisplay}"
                    : $"  {release.Sdk.VersionDisplay}");
            }
        }
    }

    public async Task Install(string version)
    {
        WriteTitle($"Install");

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
            WriteLine($"SDK version {sdkVersionToInstall} is already installed");
            return;
        }

        var releaseToInstall = releases.GetRelease(sdkVersionToInstall);
        await Install(releaseToInstall);
    }

    public async Task Uninstall(string version)
    {
        WriteTitle($"Uninstall");

        var dots = version.Count(x => x == '.');
        if (version.Contains(' ') || dots != 2) throw new ArgumentException("invalid SDK version format", nameof(version));
        await UninstallInt(version);
    }

    public async Task Upgrade(string framework)
    {
        var frameWorkVersion = framework == "all" ? "" : framework.ParseFrameworkVersion();

        WriteTitle($"Upgrade");

        // get all installed frameworks
        var localSdks = await _netSdkLocal.List();
        var frameworksToCheck = frameWorkVersion != ""
            ? new List<string> {frameWorkVersion}
            : localSdks.Select(x => x.ParseFrameworkVersion()).Distinct().ToList();

        WriteLine($"Updating .NET frameworks: {string.Join(", ", frameworksToCheck.Select(x => $"net{x}"))}");

        var releaseIndices = await _netSdkRemote.GetReleaseIndices();

        foreach (var installedFramework in frameworksToCheck)
        {
            WriteStep($"checking net{installedFramework}...");
            var releaseIndex = releaseIndices.GetReleaseIndex(installedFramework);
            if (!localSdks.Contains(releaseIndex.LatestSdk))
            {
                // upgrade
                WriteStep($"upgrading net{installedFramework} to SDK {releaseIndex.LatestSdk}...");
                var releases = await _netSdkRemote.GetReleases(releaseIndex);
                await Install(releases.GetRelease(releaseIndex.LatestSdk));

                // remove old ones
                var sdksToRemove = localSdks.Where(x => x.ParseFrameworkVersion() == installedFramework);
                foreach (var sdkToRemove in sdksToRemove)
                {
                    WriteStep($"removing SDK {sdkToRemove}...");
                    await UninstallInt(sdkToRemove);
                }
            }
            else
            {
                WriteLine($"the latest SDK is already installed for net{installedFramework}");
            }
        }
    }

    private async Task Install(Release releaseToInstall)
    {
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
            if (sdkFileInfo?.Url == null) throw new ArgumentException("SDK installation file not found", nameof(releaseToInstall));
            var pkgFile = await _netSdkRemote.DownloadFile(sdkFileInfo.Url, sdkFileInfo.Name);
            await _netSdkLocal.Run("/usr/sbin/installer", $"-pkg {pkgFile} -target /", true);
            return;
        }

        throw new Exception($"runtime identifier {_appConfig.RuntimeIdentifier} is not supported at this time");
    }

    private async Task UninstallInt(string version)
    {
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
}