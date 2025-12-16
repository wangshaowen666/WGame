/*--------------------------------------------------------------
 * File: ProcudureResCheck.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/11 14:09:47 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class ProcedureResCheck : ProcedureBase
{
    List<IResourceLocation> _totalLocation = new List<IResourceLocation>();
    
    public override void OnEnter()
    {
        base.OnEnter();
        Debug.Log("持久化数据路径: " + Application.persistentDataPath);
        // todo 先用最笨的方式实现，后续再想抽象的事情
        AsyncRun().Forget();
    }

    private async UniTaskVoid AsyncRun()
    {
        await Addressables.InitializeAsync();

        int flag = await UpdateCatalog();
        if (flag < 0) return;

        flag = await CheckRes(flag);
        if (flag == 1)
        {
            
        }
        
        await DownloadRes();
    }

    private async UniTask<int> UpdateCatalog() 
    {
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        await checkHandle;
        if (checkHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Log.Error("Catalog 检查失败");
            Addressables.Release(checkHandle);
            return -1;
        }

        int ret = 0;
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
                Log.Error("Catalog 更新失败");
                ret = -2;
            }
            else
            {
                ret = 1;
            }
            
            Addressables.Release(updateHandle);
        }
        else
        {
            
        }

        List<string> hashNmd = new List<string>();
        var locators = Addressables.ResourceLocators;
        foreach (var locator in locators)
        {
            foreach (var key in locator.Keys)
            {
                var str = key.ToString();
                if (str.StartsWith("remote_"))
                {
                    var hashposition = str.LastIndexOf("_");
                    str = str.Remove(0, hashposition + 1);
                    str = str.Remove(str.Length - 7, 7); //去除.bundle结尾
                    hashNmd.Add(str);
                }
            }
        }
        
        var cachePaths = new List<string>();
        Caching.GetAllCachePaths(cachePaths);

        foreach (var path in cachePaths.Where(Directory.Exists)
                     .SelectMany(path => Directory.EnumerateFileSystemEntries(path)))
        {
            var cachedBundleName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(cachedBundleName))
            {
                var cachedBundleVersions = new List<Hash128>();
                Caching.GetCachedVersions(cachedBundleName, cachedBundleVersions);
                foreach (var ver in cachedBundleVersions)
                {
                    var hash = ver.ToString();
                    if (!hashNmd.Contains(hash))
                    {
                        Log.Info("移除缓存文件", hash);
                        Caching.ClearCachedVersion(cachedBundleName, ver);
                    }
                }
            }
        }

        Addressables.Release(checkHandle);
        return ret;
    }

    private async UniTask<int> CheckRes(int flag)
    {
        if (flag == 0 && PlayerPrefsUtil.GetInt("UpdateFlag") == 1)
            return 0;
        
        List<string> labels = new List<string> { "common", "hd" };
        var locationHandle = Addressables.LoadResourceLocationsAsync(labels, Addressables.MergeMode.Intersection);
        await locationHandle;
        _totalLocation.AddRange(locationHandle.Result);
        
        var sizeHandle = Addressables.GetDownloadSizeAsync(_totalLocation);
        await sizeHandle;
        
        // float size = sizeHandle.Result / (1024f * 1024f);
        // Log.Info($"热更资源总大小：{size:F1}MB");
        float size = sizeHandle.Result / (1024f);
        Log.Info($"热更资源总大小：{size:F0}KB");
        
        Addressables.Release(locationHandle);
        Addressables.Release(sizeHandle);
        
        return size > 0 ? 1 : 0;
    }

    private async UniTask DownloadRes()
    {
        AsyncOperationHandle downHandle = Addressables.DownloadDependenciesAsync(_totalLocation, false);
        await downHandle;
        
        if (downHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Log.Error("下载失败，尝试重新下载");
        }
        else
        {
            Log.Error("下载成功");
        }
        
        Addressables.Release(downHandle);
    }
    
}