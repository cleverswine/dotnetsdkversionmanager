using DotnetSdkVersionManager.Interfaces;
using DotnetSdkVersionManager.Models;
using Spectre.Console;

namespace DotnetSdkVersionManager;

public class Commander
{
    private readonly AppConfig _appConfig;
    private readonly INetSdkLocal _netSdkLocal;
    private readonly INetSdkRemote _netSdkRemote;

    private static void WriteTitle(string s) =>
        AnsiConsole.MarkupLineInterpolated($"------------------{Environment.NewLine}[bold]{s}[/]{Environment.NewLine}------------------");

    private static void WriteStep(string s) => AnsiConsole.MarkupLineInterpolated($":running_shoe: {s}...");
    private static void WriteSuccess(string s = "Success") => AnsiConsole.MarkupLineInterpolated($":check_mark_button: {s}");
    private static void WriteMarkup(string s) => AnsiConsole.MarkupLine(s);
    private static void WriteLine(string s = "") => AnsiConsole.WriteLine(s);

    public Commander(AppConfig appConfig, INetSdkLocal netSdkLocal, INetSdkRemote netSdkRemote)
    {
        _appConfig = appConfig;
        _netSdkLocal = netSdkLocal;
        _netSdkRemote = netSdkRemote;
    }

    public async Task List()
    {
        WriteTitle("Installed SDKs");

        await AnsiConsole.Status()
            .StartAsync("loading list of installed SDKs...", async _ =>
            {
                var localSdks = await _netSdkLocal.List();
                var releaseIndices = await _netSdkRemote.GetReleaseIndices();
                var updates = new List<string>();

                foreach (var localSdk in localSdks)
                {
                    WriteLine("* " + localSdk);

                    var releaseIndex = releaseIndices.GetReleaseIndex(localSdk.ParseFrameworkVersion());
                    if (!localSdks.Contains(releaseIndex.LatestSdk))
                    {
                        updates.Add($"SDK {releaseIndex.LatestSdk} was released on "
                                    + $"{releaseIndex.LatestReleaseDate:yyyy-MM-dd}. "
                                    + "Use the upgrade command to install it.");
                    }
                }

                WriteLine();
                if (updates.Any())
                {
                    WriteMarkup(":package: UPDATES AVAILABLE");
                }
                else
                {
                    WriteSuccess("Everything is up to date!");
                }

                foreach (var update in updates.Distinct())
                {
                    WriteLine(update);
                }
            });
    }

    public async Task ListAvailable(string framework)
    {
        WriteTitle("Available SDKs");

        await AnsiConsole.Status()
            .StartAsync("loading list of available SDKs...", async _ =>
            {
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
            });
    }

    public async Task Install(string sdkVersion)
    {
        WriteTitle($"Install");

        var dots = sdkVersion.Count(x => x == '.');
        if (sdkVersion.Contains(' ') || dots > 2) throw new ArgumentException("invalid SDK version format", nameof(sdkVersion));

        await AnsiConsole.Status()
            .StartAsync("installing...", async _ =>
            {
                var frameworkVersion = sdkVersion.ParseFrameworkVersion();

                WriteStep("getting list of installed SDKs");
                var localSdks = await _netSdkLocal.List();

                WriteStep($"getting release list for net{frameworkVersion}");
                var releaseIndices = await _netSdkRemote.GetReleaseIndices();
                var releaseIndex = releaseIndices.GetReleaseIndex(frameworkVersion);

                WriteStep($"getting available SDKs for net{releaseIndex.ChannelVersion}");
                var releases = await _netSdkRemote.GetReleases(releaseIndex);

                var sdkToGet = dots < 2 ? releaseIndex.LatestSdk : sdkVersion;
                var releaseToInstall = releases.GetRelease(sdkToGet);

                WriteStep($"installing SDK {releaseToInstall.Sdk.VersionDisplay}");
                await Install(releaseToInstall);

                WriteSuccess();
            });
    }

    public async Task Uninstall(string sdkVersion)
    {
        WriteTitle($"Uninstall");

        var dots = sdkVersion.Count(x => x == '.');
        if (sdkVersion.Contains(' ') || dots != 2) throw new ArgumentException("invalid SDK version format", nameof(sdkVersion));

        await AnsiConsole.Status()
            .StartAsync("Uninstalling...", async _ =>
            {
                WriteStep($"removing SDK {sdkVersion}");
                await UninstallInt(sdkVersion);

                WriteSuccess();
            });
    }

    public async Task Upgrade(string framework)
    {
        var frameWorkVersion = framework == "all" ? "" : framework.ParseFrameworkVersion();

        WriteTitle($"Upgrade");

        await AnsiConsole.Status()
            .StartAsync("upgrading...", async _ =>
            {
                // get all installed frameworks
                var localSdks = await _netSdkLocal.List();
                var frameworksToCheck = frameWorkVersion != ""
                    ? new List<string> {frameWorkVersion}
                    : localSdks.Select(x => x.ParseFrameworkVersion()).Distinct().ToList();

                var releaseIndices = await _netSdkRemote.GetReleaseIndices();

                foreach (var installedFramework in frameworksToCheck)
                {
                    WriteStep($"checking net{installedFramework}");
                    var releaseIndex = releaseIndices.GetReleaseIndex(installedFramework);
                    if (!localSdks.Contains(releaseIndex.LatestSdk))
                    {
                        // upgrade
                        WriteStep($"upgrading net{installedFramework} to SDK {releaseIndex.LatestSdk}");
                        var releases = await _netSdkRemote.GetReleases(releaseIndex);
                        await Install(releases.GetRelease(releaseIndex.LatestSdk));

                        // remove old ones
                        var sdksToRemove = localSdks.Where(x => x.ParseFrameworkVersion() == installedFramework);
                        foreach (var sdkToRemove in sdksToRemove)
                        {
                            WriteStep($"removing SDK {sdkToRemove}");
                            await UninstallInt(sdkToRemove);

                            WriteSuccess();
                        }
                    }
                    else
                    {
                        WriteSuccess($"the latest SDK is already installed for net{installedFramework}");
                    }

                    WriteLine();
                }
            });
    }

    public async Task Update()
    {
        WriteTitle("Update");

        await AnsiConsole.Status()
            .StartAsync("Updating the SDK cache...", async _ =>
            {
                WriteStep("clearing local cache");
                WriteStep("downloading current SDK information");
                await _netSdkRemote.UpdateCache();
                WriteLine();
                WriteSuccess("Done");
            });
    }

    public async Task Info(string sdkVersion)
    {
        var dots = sdkVersion.Count(x => x == '.');
        if (dots > 2) throw new ArgumentException("invalid SDK version format", nameof(sdkVersion));

        await AnsiConsole.Status()
            .StartAsync($"getting info about {sdkVersion}", async _ =>
            {
                var frameworkVersion = sdkVersion.ParseFrameworkVersion();
                var localSdks = await _netSdkLocal.List();
                var releaseIndices = await _netSdkRemote.GetReleaseIndices();
                var releaseIndex = releaseIndices.GetReleaseIndex(frameworkVersion);
                var releases = await _netSdkRemote.GetReleases(releaseIndex);
                var sdkToGet = dots < 2 ? releaseIndex.LatestSdk : sdkVersion;
                var release = releases.GetRelease(sdkToGet);

                WriteTitle($"SDK {release.Sdk.Version} " + (localSdks.Contains(release.Sdk.Version ?? "") ? "(installed)" : ""));
                WriteMarkup($"[bold]Release Date[/]: {release.ReleaseDate:yyyy-MM-dd}");
                WriteMarkup($"[bold]Runtime Version[/]: {release.Runtime.VersionDisplay}");
                WriteMarkup($"[bold]C# Version[/]: {release.Sdk.CsharpVersion}");
                WriteMarkup($"[bold]F# Version[/]: {release.Sdk.FsharpVersion}");
                WriteMarkup($"[bold]VB Version[/]: {release.Sdk.VbVersion}");
                WriteMarkup($"[bold]VS Support[/]: {release.Sdk.VsSupport}");
                WriteMarkup($"[bold]VS Mac Support[/]: {release.Sdk.VsMacSupport}");
                if(release.ReleaseNotes != null) WriteMarkup($"[bold]Release Notes[/]: [link={release.ReleaseNotes}]Release Notes[/]");
                if (release.Security)
                {
                    var cveList = string.Join(", ", release.CveList.Select(x => $"[link={x.CveUrl}]{x.CveId}[/]"));
                    WriteMarkup($"[bold]CVE List[/]: {cveList}");
                }
            });
    }

    private async Task Install(Release releaseToInstall)
    {
        if (_appConfig.RuntimeIdentifier.Contains("linux"))
        {
            WriteStep("downloading linux installer");
            var installScript = await _netSdkRemote.DownloadLinuxInstaller();

            WriteStep("running linux installer");
            await _netSdkLocal.Run("chmod", $"+x {installScript}");
            await _netSdkLocal.Run(installScript, $"--version {releaseToInstall.Sdk.Version} -i /usr/share/dotnet", true);
            return;
        }

        if (_appConfig.RuntimeIdentifier.Contains("osx"))
        {
            var sdkFileInfo = releaseToInstall.Sdk.Files.FirstOrDefault(x =>
                x.Rid.Equals(_appConfig.RuntimeIdentifier, StringComparison.OrdinalIgnoreCase) && x.Name.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase));
            if (sdkFileInfo?.Url == null) throw new ArgumentException("SDK installation file not found", nameof(releaseToInstall));

            WriteStep($"downloading macos installer {sdkFileInfo.Name}");
            var pkgFile = await _netSdkRemote.DownloadFile(sdkFileInfo.Url, sdkFileInfo.Name);

            WriteStep($"running macos installer");
            await _netSdkLocal.Run("/usr/sbin/installer", $"-pkg {pkgFile} -target /", true);
            return;
        }

        throw new Exception($"runtime identifier {_appConfig.RuntimeIdentifier} is not supported at this time");
    }

    private async Task UninstallInt(string sdkVersion)
    {
        if (_appConfig.RuntimeIdentifier.Contains("linux"))
        {
            // I don't like this...
            WriteStep("removing SDK files");
            await _netSdkLocal.Run("rm", $"-rf /usr/share/dotnet/sdk/{sdkVersion}", true);
            await _netSdkLocal.Run("rm", $"-rf /usr/share/dotnet/shared/Microsoft.NETCore.App/{sdkVersion}", true);
            await _netSdkLocal.Run("rm", $"-rf /usr/share/dotnet/shared/Microsoft.AspNetCore.All/{sdkVersion}", true);
            await _netSdkLocal.Run("rm", $"-rf /usr/share/dotnet/shared/Microsoft.AspNetCore.App/{sdkVersion}", true);
            await _netSdkLocal.Run("rm", $"-rf /usr/share/dotnet/host/fxr/{sdkVersion}", true);
            return;
        }

        if (_appConfig.RuntimeIdentifier.Contains("osx"))
        {
            WriteStep($"downloading macos uninstaller");
            var uninstallScriptTarGz = await _netSdkRemote.DownloadMacOsUninstaller();
            var d = new FileInfo(uninstallScriptTarGz).DirectoryName;

            WriteStep($"running macos uninstaller");
            await _netSdkLocal.Run("tar", $"-zxf {uninstallScriptTarGz} --directory {d}");
            await _netSdkLocal.Run(uninstallScriptTarGz.Replace(".tar.gz", ""), $"remove --sdk {sdkVersion} --yes");
            return;
        }

        throw new Exception($"runtime identifier {_appConfig.RuntimeIdentifier} is not supported at this time");
    }
}