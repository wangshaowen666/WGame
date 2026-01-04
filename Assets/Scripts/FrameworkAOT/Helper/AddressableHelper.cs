/*--------------------------------------------------------------
 * File: AddressableHelper.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/16 17:14:26 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class AddressableHelper
{
    public event Action<int> OnLoadFail;
    public event Action<float, long, long> OnDownloadProgress;
    
    private const string BundleExtension = ".bundle";
    // 远端AB包的前缀
    private const string RemoteBundlePrefix = "remote_";
    private static List<string> LoginDownloadLabels = new List<string> { "common", "lua"};
    
    private List<IResourceLocation> _totalLocation = new List<IResourceLocation>();
    private long _fileSize;

    public async UniTask InitAsync()
    {
        await Addressables.InitializeAsync();
    }

    public async UniTask UpdateCatalog(CancellationToken token)
    {
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        await checkHandle;
        if (checkHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Log.Error("CheckForCatalogUpdates 执行失败", checkHandle.OperationException.Message);
            Addressables.Release(checkHandle);
            OnLoadFail?.Invoke(1);
            // 可取消标志为true时，中断try块，抛出取消
            token.ThrowIfCancellationRequested();
        }

        if (checkHandle.Result != null && checkHandle.Result.Count > 0)
        {
            Log.Info("需要更新catalog");
            PlayerPrefsUtil.SetInt("UpdateFlag", 0);
            // while (!Caching.ready)
            // {
            //     await UniTask.NextFrame();
            // }
            
            // 这里选择清除缓存，需要等缓存系统准备好，不然会报"Cache is not ready to be accessed"
            // 实测，autoCleanBundleCache为true并不能有效清理本地缓存
            var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, false);
            await updateHandle;
            if (updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error("UpdateCatalogs 执行失败", updateHandle.OperationException.Message);
                OnLoadFail?.Invoke(2);
                Addressables.Release(updateHandle);
                Addressables.Release(checkHandle);
                token.ThrowIfCancellationRequested();
            }
            else
            {
                // 每次catalog更新后，清除本地的冗余bundle
                ClearCache();
                Addressables.Release(updateHandle);
                Addressables.Release(checkHandle);
            }
        }
    }

    public async UniTask CheckRes(CancellationToken token)
    {
        if (PlayerPrefsUtil.GetInt("UpdateFlag") == 1)
            return;
        
        var locationHandle = Addressables.LoadResourceLocationsAsync(LoginDownloadLabels, Addressables.MergeMode.Intersection);
        await locationHandle;
        if (locationHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Log.Error("LoadResourceLocationsAsync 执行失败", locationHandle.OperationException.Message);
            OnLoadFail?.Invoke(3);
            Addressables.Release(locationHandle);
            token.ThrowIfCancellationRequested();
        }
        
        _totalLocation.AddRange(locationHandle.Result);
        var sizeHandle = Addressables.GetDownloadSizeAsync(_totalLocation);
        await sizeHandle;
        if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Log.Error("GetDownloadSizeAsync 执行失败", sizeHandle.OperationException.Message);
            OnLoadFail?.Invoke(4);
            Addressables.Release(locationHandle);
            Addressables.Release(sizeHandle);
            token.ThrowIfCancellationRequested();
        }
        else
        {
            _fileSize = sizeHandle.Result;
            Log.Info("热更资源总大小:", GetFileLength(_fileSize));
        
            Addressables.Release(locationHandle);
            Addressables.Release(sizeHandle);
        }
    }

    public async UniTask Download(CancellationToken token)
    {
        // var allKeys = Addressables.ResourceLocators?
        //     .Where(locator => locator?.Keys != null)
        //     .SelectMany(locator => locator.Keys);
        //
        // if (allKeys != null)
        // {
        //     foreach (var key in allKeys)
        //     {
        //         Debug.Log($"Key: {key}");
        //     }
        //     Debug.Log($"总共找到 {allKeys.Count()} 个 Key。");
        // }
        
        if (_fileSize == 0 || _totalLocation.Count == 0)
            return;
        
        AsyncOperationHandle downHandle = Addressables.DownloadDependenciesAsync(_totalLocation, false);
        var state = downHandle.GetDownloadStatus();
        while (!downHandle.IsDone)
        {
            OnDownloadProgress?.Invoke(state.Percent, state.DownloadedBytes, state.TotalBytes);
            Log.Info("下载呢", state.Percent, state.DownloadedBytes, state.TotalBytes);
            state = downHandle.GetDownloadStatus();
            await UniTask.Yield();
        }
        
        if (downHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Log.Error("GetDownloadSizeAsync 执行失败", downHandle.OperationException.Message);
            OnLoadFail?.Invoke(5);
            Addressables.Release(downHandle);
            token.ThrowIfCancellationRequested();
        }
        else
        {
            PlayerPrefsUtil.SetInt("UpdateFlag", 1);
            Addressables.Release(downHandle);
            Log.Info("下载成功");
        }
    }

    /// <summary>
    /// 移除本地不在catalog中的旧Bundle
    /// </summary>
    private void ClearCache()
    {
        var hashes = from locator in Addressables.ResourceLocators
            from key in locator.Keys
            let keyString = key.ToString()
            where keyString.StartsWith(RemoteBundlePrefix)
            select ExtractHashFromKey(keyString);

       
        var activeBundleHashes = new HashSet<string>(hashes);
        var allCachePaths = new List<string>();
        Caching.GetAllCachePaths(allCachePaths);
        
        foreach (var path in allCachePaths.Where(Directory.Exists).SelectMany(Directory.EnumerateFileSystemEntries))
        {
            var fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName)) continue;
                
            var cachedVersions = new List<Hash128>();
            Caching.GetCachedVersions(fileName, cachedVersions);
            foreach (var version in cachedVersions)
            {
                var versionHash = version.ToString();
                if (!activeBundleHashes.Contains(versionHash))
                {
                    Log.Info("移除缓存文件", versionHash);
                    Caching.ClearCachedVersion(fileName, version);
                }
            }
        }
    }
    
    /// <summary>
    /// 从资源键中提取哈希值。
    /// 例如：从 "RemoteABPrefix_1234567890abc.bundle" 中提取 "1234567890abc"。
    /// </summary>
    private string ExtractHashFromKey(string key)
    {
        var lastUnderscoreIndex = key.LastIndexOf('_');
        var hashStartIndex = lastUnderscoreIndex + 1;
        var hashLength = key.Length - hashStartIndex - BundleExtension.Length;
        return hashLength > 0 ? key.AsSpan(hashStartIndex, hashLength).ToString() : string.Empty;
    }
    
    private string GetFileLength(long byteLength)
    {
        if (byteLength < 1024L)
        {
            return $"{byteLength} B";
        }
        
        if (byteLength < 1048576L)
        {
            return $"{byteLength / 1024f:F2} KB";
        }
        
        if (byteLength < 1073741824L) // 2 ^ 30
        {
            return $"{byteLength / 1048576f:F2} MB";
        }
        
        return $"{byteLength / 1073741824f:F2} GB";
    }
}