/*--------------------------------------------------------------
 * File: IResLoader.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/09/01 10:56:06 
 *--------------------------------------------------------------
 */

using System;
using Cysharp.Threading.Tasks;

public interface IResLoader
{
    /// <summary>
    /// 同步加载资源
    /// </summary>
    T LoadSync<T>(string key);

    /// <summary>
    /// 异步加载资源
    /// </summary>
    void LoadAsync<T>(string key, LoadAssetCallback<T> callback, object userData = null);

    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    /// <param name="onProgress">加载进度回调（0~1）</param>
    /// <param name="onComplete">加载完成回调</param>
    void LoadSceneAsync(string sceneName, Action<float> onProgress = null, Action onComplete = null);

    /// <summary>
    /// 卸载指定key的资源
    /// </summary>
    void Unload(string key);

    /// <summary>
    /// 卸载所有已加载的资源
    /// </summary>
    void UnloadAll();

#if STATS_ON && UNITY_EDITOR
    System.Collections.Generic.List<string> DealPoolStats();
#endif
}