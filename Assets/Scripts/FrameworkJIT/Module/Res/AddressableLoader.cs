/*--------------------------------------------------------------
 * File: AddressableLoader.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/09/01 11:00:17 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableLoader : IResLoader
{
    private readonly Dictionary<string, AsyncOperationHandle> _handleMap = new();
    // Addressable内部也会引用计数，但是没有忽略计数直接卸载的方法，我们也拿不到它的内部计数，所以自己维护了一个计数
    private readonly Dictionary<string, int> _refMap = new();

    public T LoadSync<T>(string key)
    {
#if STATS_ON && UNITY_EDITOR
        UpdateStats(key,1);
#endif

        if (_handleMap.TryGetValue(key, out AsyncOperationHandle tempHandle))
        {
            _refMap.TryGetValue(key, out int c);
            _refMap[key] = c + 1;
            return (T)tempHandle.Result;
        }

        var handle = Addressables.LoadAssetAsync<T>(key);
        T ret = handle.WaitForCompletion();
        _handleMap.TryAdd(key, handle);
        _refMap.TryGetValue(key, out int count);
        _refMap[key] = count + 1;

        return ret;
    }

    public void LoadAsync<T>(string key, LoadAssetCallback<T> callback = null, object userData = null)
    {
#if STATS_ON && UNITY_EDITOR
        UpdateStats(key,1);
#endif
        LoadRes(key, callback, userData).Forget();
    }

    private async UniTaskVoid LoadRes<T>(string key, LoadAssetCallback<T> callback = null, object userData = null)
    {
        try
        {
            if (_handleMap.TryGetValue(key, out AsyncOperationHandle tempHandle))
            {
                _refMap.TryGetValue(key, out int c);
                _refMap[key] = c + 1;
                callback?.Invoke((T)tempHandle.Result, userData);
                return;
            }

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"资源加载失败:{key}  {handle.OperationException}");

            _handleMap.TryAdd(key, handle);
            _refMap.TryGetValue(key, out int count);
            _refMap[key] = count + 1;

            callback?.Invoke(handle.Result, userData);
        }
        catch (Exception e)
        {
            Log.Error($"资源异步加载异常: {key}", e);
        }
    }

    public void Unload(string key)
    {
        if (!_handleMap.TryGetValue(key, out var handle))
        {
            Log.Error("要卸载的资源不存在", key);
            return;
        }

#if STATS_ON && UNITY_EDITOR
        UpdateStats(key,2, 1);
#endif

        int count = _refMap[key] - 1;
        if (count <= 0)
        {
            _handleMap.Remove(key);
            _refMap.Remove(key);
            Addressables.Release(handle);
        }
        else
        {
            _refMap[key] = count;
        }
    }

    public void UnloadAll()
    {
        foreach (var kv in _handleMap)
        {
            if (kv.Value.IsValid() && _refMap.TryGetValue(kv.Key, out int count))
            {
                if (count > 0)
                {
                    Addressables.Release(kv.Value);
#if STATS_ON && UNITY_EDITOR
                    UpdateStats(kv.Key,2, count);
#endif
                }
            }
        }

        _handleMap.Clear();
        _refMap.Clear();
    }

    public int GetLoadedCount(string key)
    {
        _refMap.TryGetValue(key, out int count);
        return count;
    }

    public async UniTaskVoid PreloadWithLabel<T>(string label, Action<T> callback = null, object userData = null)
    {
        var handle = Addressables.LoadAssetsAsync<T>(label, callback);
        await handle.Task;

        foreach (var asset in handle.Result)
        {
            Log.Info("资源加载成功：", asset);
        }
    }
    
#if STATS_ON && UNITY_EDITOR
    private readonly Dictionary<string, AssetBundleStats> _statsMap = new();

    /// <summary>
    /// 更新统计信息
    /// </summary>
    /// <param name="key">bundle名</param>
    /// <param name="flag">操作标识。1获取 2释放</param>
    /// <param name="count">数量</param>
    private void UpdateStats(string key, int flag, int count = 1)
    {
        if (!_statsMap.TryGetValue(key, out var stats))
        {
            stats = new AssetBundleStats();
            _statsMap[key] = stats;
        }
        switch (flag)
        {
            case 1:
                stats.currentNum += count;
                stats.totalGets += count;

                if (stats.peakNUm < stats.currentNum)
                    stats.peakNUm = stats.currentNum;
                break;

            case 2:
                stats.currentNum -= count;
                stats.totalPuts -= count;
                break;
        }
    }

    public List<string> DealPoolStats()
    {
        List<string> result = new List<string>();
        foreach (var statsKV in _statsMap)
        {
            var stats = statsKV.Value;
            result.Add($"{statsKV.Key},{stats.currentNum},{stats.peakNUm}," +
                       $"{stats.totalGets},{stats.totalPuts}");
        }

        return result;
    }
#endif
}

#if STATS_ON && UNITY_EDITOR
/// <summary>
/// 对象池统计信息
/// </summary>
public class AssetBundleStats
{
    public int currentNum;      // 当前引用数量
    public int peakNUm;         // 峰值引用数量
    public long totalGets;      // 总获取次数
    public long totalPuts;      // 总释放次数
}
#endif