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
    private readonly IResLoader _resLoader = new AddressableLoader();
    private readonly Dictionary<string, MethodInfo> _genericMap = new();
    
    // 图集有不同配置的时候使用
    // SpriteAtlasManager.atlasRequested -= RequestAtlas;
    // SpriteAtlasManager.atlasRequested += RequestAtlas;

    private void RequestAtlas(string tag, LoadAssetCallback<SpriteAtlas> callback)
    {
        Log.Info("要加载图集了：", tag);
        var b = LoadSync<SpriteAtlas>(tag);
        FrameworkMgr.Timer.StartDelay(5000, i =>
        {
            callback(b, null);
        });
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
    
    public void LoadPrefab(string key, LoadAssetCallback<GameObject> callback, object userData = null)
    {
        LoadAsync(key, callback, userData);
    }

    /// <summary>
    /// 提供给Lua的接口，只加载Unity资产，继承自UnityEngine.Object
    /// </summary>
    /// <param name="type">资源类型</param>
    /// <param name="key">资源名称</param>
    /// <param name="callback">回调</param>
    /// <exception cref="GameException">类型不存在</exception>
    public void LoadRes(string type, string key, Action<Object> callback)
    {
        if (!_genericMap.TryGetValue(type, out MethodInfo info))
        {
            Assembly unityEngineCore = Assembly.Load("UnityEngine.CoreModule");
            Type tp = unityEngineCore.GetType($"UnityEngine.{type}");
            if (tp == null)
            {
                Debug.Log(typeof(Sprite).AssemblyQualifiedName);
                throw new Exception("不存在的类型" + type);
            }
            
            var methodInfo = typeof(IResLoader).GetMethod("LoadAsync");
            if (methodInfo != null)
            {
                info = methodInfo.MakeGenericMethod(tp);
                _genericMap.Add(type, info);
            }
        }

        if (info != null)
        {
            info.Invoke(_resLoader, new object[] { key, callback });
        }
        else
        {
            Log.Error("反射加载类型出错：", type);
        }
    }
    
#if STATS_ON && UNITY_EDITOR
    public List<string> DealPoolStats()
    {
        return _resLoader.DealPoolStats();
    }
#endif
}