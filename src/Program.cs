using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using DotnetSdkVersionManager;
using DotnetSdkVersionManager.Services;
using Spectre.Console;

// set up services
var appConfig = new AppConfig();
var httpClient = new HttpClient();
var cache = new FileSystemCache(appConfig);
var netSdkLocal = new NetSdkLocal(appConfig);
var netSdkRemote = new NetSdkRemote(httpClient, cache);
var commander = new Commander(appConfig, netSdkLocal, netSdkRemote);

// parse command line and execute command
await ConfigureCommandLine(commander).InvokeAsync(args);

// set up command line parsing using System.CommandLine
Parser ConfigureCommandLine(Commander cmd)
{
    var rootCmd = new RootCommand(".NET SDK Version Manager");

    var sdkVersionArg = new Argument<string>("sdk-version", "the SDK version");

    var frameworkOption = new Option<string>("--framework", "limit the SDK list to a specific framework");
    frameworkOption.SetDefaultValue("all");
    frameworkOption.AddAlias("-f");

    var listCmd = new Command("list", "lists installed SDKs");
    listCmd.SetHandler(cmd.List);
    rootCmd.Add(listCmd);

    var upgradeCmd = new Command("upgrade", "upgrade to the latest SDK (must be run as root)") {frameworkOption};
    upgradeCmd.SetHandler(cmd.Upgrade, frameworkOption);
    rootCmd.Add(upgradeCmd);

    var listAvailableCmd = new Command("list-available", "lists available SDKs") {frameworkOption};
    listAvailableCmd.SetHandler(cmd.ListAvailable, frameworkOption);
    rootCmd.Add(listAvailableCmd);

    var installCmd = new Command("install", "install a SDK (must be run as root)") {sdkVersionArg};
    installCmd.SetHandler(cmd.Install, sdkVersionArg);
    rootCmd.Add(installCmd);

    var uninstallCmd = new Command("uninstall", "uninstall a SDK (must be run as root)") {sdkVersionArg};
    uninstallCmd.SetHandler(cmd.Uninstall, sdkVersionArg);
    rootCmd.Add(uninstallCmd);

    var infoCmd = new Command("info", "show information about a SDK") {sdkVersionArg};
    infoCmd.SetHandler(cmd.Info, sdkVersionArg);
    rootCmd.Add(infoCmd);

    var updateCmd = new Command("update", "update the SDK cache");
    updateCmd.SetHandler(cmd.Update);
    rootCmd.Add(updateCmd);

    const string examples = """

Examples:

    List installed SDKs
    > dvm list

    upgrade to the latest SDK available for all installed frameworks
    > sudo dvm upgrade

    upgrade to the latest SDK available for the .NET 6.0 framework
    > sudo dvm upgrade -f net6.0

    List all available SDKs for the .NET 7.0 framework
    > dvm list-available -f net7.0

    install a specific SDK version
    > sudo dvm install 7.0.102
""";

    var parser = new CommandLineBuilder(rootCmd)
        .UseDefaults()
        .UseExceptionHandler((exception, _) =>
        {
            if (exception is not ArgumentException)
            {
                AnsiConsole.MarkupLine(":red_exclamation_mark: Oops, something bad happened:");
                AnsiConsole.MarkupLineInterpolated($":red_exclamation_mark: => {exception.Message}");
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated($":white_exclamation_mark: {exception.Message}");
            }
        })
        .UseHelp(ctx =>
        {
            if (ctx.Command.Name == "dvm")
                ctx.HelpBuilder
                    .CustomizeLayout(_ =>
                    {
                        return HelpBuilder.Default.GetLayout()
                                .Prepend(_ => AnsiConsole.Write(new FigletText("dvm")))
                                .Append(_ => Console.WriteLine(examples))
                            ;
                    });
        })
        .Build();

    return parser;
}