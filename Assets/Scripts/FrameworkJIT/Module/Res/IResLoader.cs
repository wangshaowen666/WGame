/*--------------------------------------------------------------
 * File: IResLoader.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/09/01 10:56:06 
 *--------------------------------------------------------------
 */

public interface IResLoader
{
    /// <summary>
    /// 同步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="key">资源键</param>
    /// <returns>加载的资源</returns>
    T LoadSync<T>(string key);
    
    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="key">资源键</param>
    /// <param name="callback">加载完成回调</param>
    /// <param name="userData">用户数据</param>
    void LoadAsync<T>(string key, LoadAssetCallback<T> callback, object userData = null);
    
    /// <summary>
    /// 卸载指定key的所有资源
    /// </summary>
    /// <param name="key">资源键</param>
    void Unload(string key);
    
    /// <summary>
    /// 卸载所有已加载的资源
    /// </summary>
    void UnloadAll();
    
#if STATS_ON && UNITY_EDITOR
    public System.Collections.Generic.List<string> DealPoolStats();
#endif
}