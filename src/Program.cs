using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using DotnetSdkVersionManager;

var appConfig = new AppConfig();
var httpClient = new HttpClient();
var cache = new FileSystemCache(appConfig);
var netSdkLocal = new NetSdkLocal(appConfig);
var netSdkRemote = new NetSdkRemote(httpClient, cache);

await ConfigureCommandLine(new NetSdk(appConfig, netSdkLocal, netSdkRemote)).InvokeAsync(args);

Parser ConfigureCommandLine(NetSdk n)
{
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

    var rootCmd = new RootCommand(".NET SDK Version Manager");

    var sdkVersionArg = new Argument<string>("sdk-version", "the SDK version");

    var frameworkOption = new Option<string>("--framework", "limit the SDK list to a specific framework");
    frameworkOption.SetDefaultValue("all");
    frameworkOption.AddAlias("-f");

    var listCmd = new Command("list", "lists installed SDKs");
    listCmd.SetHandler(n.List);
    rootCmd.Add(listCmd);

    var upgradeCmd = new Command("upgrade", "upgrade to the latest SDK (must be run as root)") {frameworkOption};
    upgradeCmd.SetHandler(n.Upgrade, frameworkOption);
    rootCmd.Add(upgradeCmd);
    
    var listAvailableCmd = new Command("list-available", "lists available SDKs") {frameworkOption};
    listAvailableCmd.SetHandler(n.ListAvailable, frameworkOption);
    rootCmd.Add(listAvailableCmd);

    var installCmd = new Command("install", "install a SDK (must be run as root)") {sdkVersionArg};
    installCmd.SetHandler(n.Install, sdkVersionArg);
    rootCmd.Add(installCmd);

    var uninstallCmd = new Command("uninstall", "uninstall a SDK (must be run as root)") {sdkVersionArg};
    uninstallCmd.SetHandler(n.Uninstall, sdkVersionArg);
    rootCmd.Add(uninstallCmd);

    var parser = new CommandLineBuilder(rootCmd)
        .UseDefaults()
        .UseHelp(ctx =>
        {
            if (ctx.Command.Name == "dvm")
                ctx.HelpBuilder
                    .CustomizeLayout(_ =>
                    {
                        return HelpBuilder.Default.GetLayout()
                                //.Prepend(_ => AnsiConsole.Write(new FigletText("dvm")))
                                .Append(_ => Console.WriteLine(examples))
                            ;
                    });
        })
        .Build();

    return parser;
}