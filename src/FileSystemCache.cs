using System.Text.Json;
using DotnetSdkVersionManager.Interfaces;

namespace DotnetSdkVersionManager;

public class FileSystemCache : ISimpleCache
{
    private readonly string _basePath;
    private readonly int _expiryMinutes;

    public FileSystemCache(AppConfig appConfig)
    {
        _basePath = Path.Combine(appConfig.AppDataPath, "dvm", ".cache");
        if (!Directory.Exists(_basePath)) Directory.CreateDirectory(_basePath);
        _expiryMinutes = 10;
    }

    public async Task<T?> GetOrCreate<T>(string key, Func<Task<T>> f)
    {
        var fi = new FileInfo(Path.Join(_basePath, key));
        
        if (fi.Exists && fi.LastWriteTime >= DateTime.Now.AddMinutes(-_expiryMinutes))
        {
            await using var rStream = fi.OpenRead();
            return await JsonSerializer.DeserializeAsync<T>(rStream);
        }

        var value = await f();
        await using var wStream = fi.OpenWrite();
        await JsonSerializer.SerializeAsync(wStream, value);
        return value;
    }

    public async Task Clean()
    {
        await Task.Yield();
        Directory.Delete(_basePath, true);
        Directory.CreateDirectory(_basePath);
    }
}