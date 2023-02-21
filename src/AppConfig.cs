using System.Runtime.InteropServices;

namespace DotnetSdkVersionManager;

public class AppConfig
{
    public AppConfig()
    {
        DotnetCommand = DotnetCmd();
        RuntimeIdentifier = Rid();
        AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }

    public string RuntimeIdentifier { get; }
    public string DotnetCommand { get; }
    public string AppDataPath { get; }

    private string DotnetCmd()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "dotnet";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "/usr/bin/dotnet";
        if (RuntimeInformation.RuntimeIdentifier.Contains("osx") || RuntimeInformation.RuntimeIdentifier.Contains("mac"))
            return "/usr/local/share/dotnet/dotnet";
        throw new Exception($"could not determine dotnet command for {RuntimeInformation.RuntimeIdentifier}");
    }

    private string Rid()
    {
        if (RuntimeInformation.RuntimeIdentifier.Contains("osx") || RuntimeInformation.RuntimeIdentifier.Contains("mac"))
            return
                RuntimeInformation.OSArchitecture is Architecture.Arm or Architecture.Arm64 or Architecture.Armv6
                    ? "osx-arm64"
                    : "osx-x64";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (Environment.Is64BitOperatingSystem)
                return
                    RuntimeInformation.OSArchitecture is Architecture.Arm or Architecture.Arm64 or Architecture.Armv6
                        ? "linux-arm64"
                        : "linux-x64";

            return
                RuntimeInformation.OSArchitecture is Architecture.Arm or Architecture.Arm64 or Architecture.Armv6
                    ? "linux-arm"
                    : "";
        }

        throw new Exception($"could not determine runtime identifier for {RuntimeInformation.RuntimeIdentifier}");
    }
}