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

public class ResMgr : ManagerBase
{
    private IResLoader _resLoader;

    public override void OnInit()
    {
        base.OnInit();
        _resLoader = new AddressableLoader();
    }

    public T LoadSync<T>(string key) => _resLoader.LoadSync<T>(key);

    public UniTask<T> LoadAsync<T>(string key)
        => _resLoader.LoadAsync<T>(key);

    public UniTask<IList<T>> PreloadWithLabel<T>(string label)
        => _resLoader.PreloadWithLabel(label);

    public void LoadSceneAsync(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        => _resLoader.LoadSceneAsync(sceneName, onProgress, onComplete);

    public void Unload(string key) => _resLoader.Unload(key);

    public void UnloadAll() => _resLoader.UnloadAll();

#if STATS_ON && UNITY_EDITOR
    public List<string> DealPoolStats() => _resLoader.DealPoolStats();
#endif
}