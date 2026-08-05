/*--------------------------------------------------------------
 * File: ResMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/09/01 10:55:17 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

// in关键字指定泛型类型参数T是逆变的，这意味着委托之间的赋值兼容性方向与类型继承方向相反，不加in需要类型完全匹配
public delegate void LoadAssetCallback<in T>(T asset, object userData);
public class ResMgr : ManagerBase
{
    private IResLoader _resLoader;
    
    // 图集有不同配置的时候使用
    // SpriteAtlasManager.atlasRequested -= RequestAtlas;
    // SpriteAtlasManager.atlasRequested += RequestAtlas;

    // private void RequestAtlas(string tag, LoadAssetCallback<SpriteAtlas> callback)
    // {
    //     var b = LoadSync<SpriteAtlas>(tag);
    //     CoreMgr.Timer.StartDelay(5000, () =>
    //     {
    //         callback(b, null);
    //     });
    // }

    public override void OnInit()
    {
        base.OnInit();
        _resLoader = new AddressableLoader();
    }

    public T LoadSync<T>(string key)
    {
        return _resLoader.LoadSync<T>(key);
    }

    public void LoadAsync<T>(string key, LoadAssetCallback<T> callback = null, object userData = null)
    {
        _resLoader.LoadAsync(key, callback, userData);
    }

    public void Unload(string key)
    {
        _resLoader.Unload(key);
    }

    public void UnloadAll()
    {
        _resLoader.UnloadAll();
    }
    
#if STATS_ON && UNITY_EDITOR
    public List<string> DealPoolStats()
    {
        return _resLoader.DealPoolStats();
    }
#endif
}