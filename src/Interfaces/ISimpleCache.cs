namespace DotnetSdkVersionManager.Interfaces;

public interface ISimpleCache
{
    Task<T?> GetOrCreate<T>(string key, Func<Task<T>> f);
    Task Clean();
}