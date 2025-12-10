/*--------------------------------------------------------------
 * File: AddressableLoader.cs
 * Author: Wang ShaoWen
 * Time: 2025/09/01 11:00:17 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableLoader : IResLoader
{
    public T LoadSync<T>(string key) where T : class
    {
        Log.Error("Addressable只有异步加载的方法");
        return null;
    }

    public void LoadAsync<T>(string key, Action<T> callback) where T : class
    {
        CoroutineRunner.Instance.Run(LoadRes<T>(key, callback));
    }

    public void LoadAsyncWithLabel(string label)
    {
        
    }
    
    private IEnumerator LoadRes<T>(string key, Action<T> callback) where T : class
    {
        Log.Info("Key:", key, typeof(T).Name);
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Log.Error("资源加载失败", key, handle.OperationException.ToString());
        }
        else
        {
            callback?.Invoke(handle.Result);
        }
        
        Addressables.Release(handle);
    }
    
    private IEnumerator LoadResWithLabel(string label)
    {
        AsyncOperationHandle<IList<UnityEngine.Object>> handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(label, null);
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Log.Error("资源加载失败", label, handle.OperationException.ToString());
        }
        else
        {
            IList<UnityEngine.Object> loadedAssets = handle.Result;
            foreach (var asset in loadedAssets)
            {
                Debug.Log(asset.name);
            }
        }
        
        Addressables.Release(handle);
    }
    
}