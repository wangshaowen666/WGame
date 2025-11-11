/*--------------------------------------------------------------
 * File: HotUpdater.cs
 * Author: Wang ShaoWen
 * Time: 2025/09/01 14:55:10 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class HotUpdater : MonoBehaviour
{
    public enum UpdateState
    {
        None,
        Address,
        Version,
        Update,
        Download,
        StopDownLoad
    }

    [SerializeField]
    public bool HotUpdate = true;
    [SerializeField]
    public string VersionURL = "CDN地址";
    [SerializeField]
    public string Catalog = "Catalog";
    
    // 资源检查出错
    public Action<string> OnVersionCheckError;
    // 资源更新出错
    public Action<string> OnVersionUpdateError;
    // 获取资源下载大小
    public Action<long> OnResourceSize;
    // 断网回调
    public Action OnDisconnectNet;
    // 重新联网回调
    public Action OnReconnectNet;
    // 下载进度回调
    public Action<float> OnDownloadProgress;
    // 更新完成
    public Action OnUpdateFinish;

    // 最大尝试下载次数
    private const int MaxDownloadCount = 3;
    
    private UpdateState _state = UpdateState.None;
    private readonly List<object> _updateKeys = new List<object>();
    private Coroutine _downloadCoroutine;
    private int _curDownloadCount;
    private NetworkReachability _lastNet;

    private void Awake()
    {
        _lastNet = Application.internetReachability;
    }

    private void Start()
    {
        if (HotUpdate)
        {
            StartCoroutine(SetAddress());
        }
        else
        {
            UpdateFinish();
        }
    }

    private void Update()
    {
        if (Application.internetReachability != _lastNet)
        {
            switch (Application.internetReachability)
            {
                case NetworkReachability.NotReachable:
                    if (_state == UpdateState.Download)
                        StopDownload();
                    
                    OnDisconnectNet?.Invoke();
                    Log.Info("网络连接断开");
                    break;
                
                case NetworkReachability.ReachableViaCarrierDataNetwork:
                case NetworkReachability.ReachableViaLocalAreaNetwork:
                    OnReconnectNet?.Invoke();
                    TryAgain();
                    break;
            }
            _lastNet = Application.internetReachability;
        }
    }

    private IEnumerator SetAddress()
    {
        _state = UpdateState.Address;
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            OnDisconnectNet?.Invoke();
            yield break;
        }

        // using (UnityWebRequest request = new UnityWebRequest(VersionURL))
        // {
        //     yield return request.SendWebRequest();
        // }
        
        //UnityWebRequest request = UnityWebRequest.Post(VersionURL, "");

        StartCoroutine(ResCheck());
    }

    private IEnumerator ResCheck()
    {
        _state = UpdateState.Version;
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            OnDisconnectNet?.Invoke();
            yield break;
        }
        AsyncOperationHandle<IResourceLocator> initHandle = Addressables.InitializeAsync();
        yield return initHandle;

        // 句柄无效，Player Settings > Other Settings下勾选 Allow 'unsafe' HTTP requests
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;

        if (checkHandle.Status != AsyncOperationStatus.Succeeded)
        {
            OnVersionCheckError?.Invoke(checkHandle.OperationException.ToString());
            Log.Error("版本检测失败：" + checkHandle.OperationException.ToString());
            yield break;
        }

        if (checkHandle.Result.Count > 0)
        {
            Log.Info("需要热更");
            _state = UpdateState.Update;
            AsyncOperationHandle<List<IResourceLocator>> updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, false);
            yield return updateHandle;
            if (updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                OnVersionUpdateError?.Invoke(updateHandle.OperationException.ToString());
                Log.Error("版本更新失败：" + checkHandle.OperationException.ToString());
                yield break;
            }
            // 更新迭代器列表
            List<IResourceLocator> locators = updateHandle.Result;
            foreach (var locator in locators)
            {
                _updateKeys.AddRange(locator.Keys);
            }
            
            Addressables.Release(checkHandle);
            Addressables.Release(updateHandle);
        }
        // 版本已经更新过
        else
        {
            // 获取所有本地文件的资源地址或标签
            IEnumerable<IResourceLocator> locators = Addressables.ResourceLocators;
            foreach (var locator in locators)
            {
                _updateKeys.AddRange(locator.Keys);
            }
        }

        AsyncOperationHandle<long> sizeHandle = Addressables.GetDownloadSizeAsync(_updateKeys as IEnumerable<object>);
        yield return sizeHandle;
        Log.Info("热更资源总大小：" + sizeHandle.Result);

        if (sizeHandle.Result > 0)
        {
            OnResourceSize?.Invoke(sizeHandle.Result);
            StartDownload();
        }
        else
        {
            Log.Info("未检测到更新，无需下载");
            UpdateFinish();
        }
        
        Addressables.Release(sizeHandle);
    }

    private IEnumerator Download()
    {
        _state = UpdateState.Download;
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            OnDisconnectNet?.Invoke();
            yield break;
        }
        // Union为取所有指定键的并集，多个不同的标签全部下载
        // Intersection取交集，只会下载与所有键都关联的资源
        // UseFirst取第一个键的所有标签
        // 会比较本地与远端的哈希值，智能下载
        AsyncOperationHandle downHandle = Addressables.DownloadDependenciesAsync(_updateKeys as IEnumerable<object>, Addressables.MergeMode.Union, false);
        while (!downHandle.IsDone)
        {
            float bar = downHandle.GetDownloadStatus().Percent;
            OnDownloadProgress?.Invoke(bar);
            yield return null;
        }

        yield return downHandle;
        if (downHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Log.Error("下载失败，尝试重新下载");
            StartDownload();
            Addressables.Release(downHandle);
            yield break;
        }
        else
        {
            Addressables.Release(downHandle);
            UpdateFinish();
        }
    }

    private void StartDownload()
    {
        if (_curDownloadCount > MaxDownloadCount)
        {
            Log.Info(Log.LogColor.Red, "下载失败次数过多");
            return;
        }

        _curDownloadCount++;
        _downloadCoroutine = StartCoroutine(Download());
    }

    private void StopDownload()
    {
        _state = UpdateState.StopDownLoad;
        StopCoroutine(_downloadCoroutine);
    }

    private void UpdateFinish()
    {
        Log.Info("更新完成");
        OnUpdateFinish?.Invoke();
    }

    private void TryAgain()
    {
        switch (_state)
        {
            case UpdateState.None:
                break;
            
            case UpdateState.Address:
                StartCoroutine(SetAddress());
                break;
            
            case UpdateState.Version:
            case UpdateState.Update:
                StartCoroutine(ResCheck());
                break;
            
            case UpdateState.Download:
            case UpdateState.StopDownLoad:
                StartDownload();
                break;
        }
    }
}