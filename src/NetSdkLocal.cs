using System.Text;
using CliWrap;
using CliWrap.Buffered;
using DotnetSdkVersionManager.Interfaces;

namespace DotnetSdkVersionManager;

public class NetSdkLocal : INetSdkLocal
{
    private readonly AppConfig _appConfig;
    public NetSdkLocal(AppConfig appConfig)
    {
        _appConfig = appConfig;
    }
    
    public async Task<List<string>> List()
    {
        List<string> result = new();
        var s = await Run(_appConfig.DotnetCommand, "--list-sdks");
        if (string.IsNullOrEmpty(s)) return result;
        var sdks = s.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        return sdks.Select(x => x.Split('[', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0]).ToList();
    }
    
    public async Task<string> Run(string command, string args, bool runAsRoot = false)
    {
        if (runAsRoot)
        {
            var tmp = Path.GetTempPath();
            var fi = new FileInfo(Path.Combine(tmp, "root-test.sh"));
            if (!fi.Exists)
            {
                await File.WriteAllTextAsync(fi.FullName, RootTest);
                await Cli.Wrap("chmod").WithArguments($"+x {fi.FullName}").ExecuteAsync();
            }
            Console.WriteLine($"trying to run: {fi.FullName}");
            var rootResult = await Cli.Wrap(fi.FullName)
                .WithValidation(CommandResultValidation.None)
                .WithWorkingDirectory(fi.DirectoryName ?? "")
                .ExecuteAsync();
            if (rootResult.ExitCode != 0) throw new Exception("This command must be run as root.");
        }

        var commandResult = await Cli.Wrap(command)
            .WithArguments(args)
            .ExecuteBufferedAsync(Encoding.UTF8);

        return commandResult.StandardOutput;
    }

    private const string RootTest = """
#!/usr/bin/env bash

if [ $EUID -ne 0 ]; then
	exit 1
fi
""";    
}