/*--------------------------------------------------------------
 * File: ResMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/09/01 10:55:17 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

// in关键字指定泛型类型参数T是逆变的，这意味着委托之间的赋值兼容性方向与类型继承方向相反，不加in需要类型完全匹配
public delegate void LoadAssetCallback<in T>(T asset, object userData);

public class ResMgr : ManagerBase
{
    private IResLoader _resLoader;

    public override void OnInit()
    {
        base.OnInit();
        _resLoader = new AddressableLoader();
    }

    public T LoadSync<T>(string key) => _resLoader.LoadSync<T>(key);

    public void LoadAsync<T>(string key, LoadAssetCallback<T> callback = null, object userData = null)
        => _resLoader.LoadAsync(key, callback, userData);

    public void LoadSceneAsync(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        => _resLoader.LoadSceneAsync(sceneName, onProgress, onComplete);

    public void Unload(string key) => _resLoader.Unload(key);

    public void UnloadAll() => _resLoader.UnloadAll();

#if STATS_ON && UNITY_EDITOR
    public List<string> DealPoolStats() => _resLoader.DealPoolStats();
#endif
}