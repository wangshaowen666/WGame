/*--------------------------------------------------------------
 * File: AddressableLoader.cs
 * Author: Wang ShaoWen
 * Time: 2025/09/01 11:00:17 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableLoader : IResLoader
{
    private readonly Dictionary<string, HandleInfo> _handleMap = new Dictionary<string, HandleInfo>();
    
    /// <summary>
    /// 推荐使用异步方式，同步加载目前只有Lua的CustomLoad注册使用了、LuBan加载配置表也用到了
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T LoadSync<T>(string key)
    {
        if (_handleMap.TryGetValue(key, out var innerHandle))
        {
            if (innerHandle.Handle.IsValid())
            {
                innerHandle.Count++;
                return (T)innerHandle.Handle.Result;
            }
            
            _handleMap.Remove(key);
        }
        
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
        T ret = handle.WaitForCompletion();;
        _handleMap.Add(key, HandleInfo.Create(handle, 1));
        return ret;
    }

    public void LoadAsync<T>(string key, LoadAssetCallback<T> callback = null, object userData = null)
    {
        LoadRes(key, callback, userData).Forget();
    }

    private async UniTaskVoid LoadRes<T>(string key, LoadAssetCallback<T> callback = null, object userData = null)
    {
        if (_handleMap.TryGetValue(key, out var innerHandle))
        {
            if (innerHandle.Handle.IsValid())
            {
                callback?.Invoke((T)innerHandle.Handle.Result, userData);
                innerHandle.Count++;
                return;
            }
            
            _handleMap.Remove(key);
        }
        
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
        await handle.Task;
        
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            throw new GameException($"资源加载失败:{key}  {handle.OperationException}");
        }
        
        _handleMap.Add(key, HandleInfo.Create(handle, 1));
        callback?.Invoke(handle.Result, userData);
    }
    
    /// <summary>
    /// 卸载指定key的所有资源
    /// </summary>
    /// <param name="key">资源键</param>
    public void Unload(string key)
    {
        if (!_handleMap.TryGetValue(key, out var handleInfo))
        {
            Log.Error("要移除的资源 bundle不存在", key);
            return;
        }

        if (handleInfo.Count > 1)
        {
            handleInfo.Count--;
        }
        else
        {
            Addressables.Release(handleInfo.Handle);
            ClassPool.Recycle(handleInfo);
            _handleMap.Remove(key);
        }
    }
    
    /// <summary>
    /// 卸载所有已加载的资源
    /// </summary>
    public void UnloadAll()
    {
        foreach (var handle in _handleMap.Values)
        {
            Addressables.Release(handle);
        }
        
        _handleMap.Clear();
    }

    public int GetLoadedCount(string key)
    {
        if (_handleMap.TryGetValue(key, out var handleInfo))
        {
            return handleInfo.Count;
        }
        
        return 0;
    }

    // 按标签加载，暂时没用
    public async UniTaskVoid PreloadWithLabel<T>(string label, Action<T> callback = null, object userData = null)
    {
        var handle = Addressables.LoadAssetsAsync<T>(label, callback);
        await handle.Task;

        foreach (var asset in handle.Result)
        {
            Log.Info("资源加载成功：", asset);
        }
    }
    
    private class HandleInfo : IResetable
    {
        public AsyncOperationHandle Handle;
        public int Count;

        public static HandleInfo Create(AsyncOperationHandle handle, int count)
        {
            var info = ClassPool.Get<HandleInfo>();
            info.Handle = handle;
            info.Count = count;
            
            return info;
        }

        public void Reset()
        {
            Handle = default;
            Count = 0;
        }
    }
}