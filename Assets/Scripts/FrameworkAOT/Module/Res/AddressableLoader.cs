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
    /// <summary>
    /// 推荐使用异步方式，同步加载目前只有Lua的CustomLoad注册使用了
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T LoadSync<T>(string key)
    {
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
        if (handle.IsDone)
        {
            return handle.Result;
        }

        var ret = handle.WaitForCompletion();
        //Addressables.Release(handle);
        return ret;
    }

    public void LoadAsync<T>(string key, Action<T> callback)
    {
        LoadRes<T>(key, callback).Forget();
    }

    private async UniTaskVoid LoadRes<T>(string key, Action<T> callback = null)
    {
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
        await handle.Task;
        
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            throw new GameException($"资源加载失败:{key}  handle.OperationException.ToString()");
        }
        
        callback?.Invoke(handle.Result);
        //Addressables.Release(handle);
    }
}