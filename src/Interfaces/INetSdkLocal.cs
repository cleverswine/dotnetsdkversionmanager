namespace DotnetSdkVersionManager.Interfaces;

public interface INetSdkLocal
{
    Task<List<string>> List();
    Task<string> Run(string command, string args, bool asRoot = false);
}