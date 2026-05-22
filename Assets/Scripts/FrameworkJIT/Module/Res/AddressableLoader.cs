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
                throw new GameException($"资源加载失败:{key}  {handle.OperationException}");

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

        Addressables.Release(handle);
        int count = _refMap[key] - 1;

        if (count <= 0)
        {
            _handleMap.Remove(key);
            _refMap.Remove(key);
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
                for (int i = 0; i < count; i++)
                    Addressables.Release(kv.Value);
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
}