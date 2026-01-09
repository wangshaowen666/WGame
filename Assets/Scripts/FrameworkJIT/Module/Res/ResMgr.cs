/*--------------------------------------------------------------
 * File: ResMgr.cs
 * Author: Wang ShaoWen
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

public class ResMgr : Singleton<ResMgr>
{
    private readonly IResLoader _resLoader;
    private readonly Dictionary<string, MethodInfo> _genericMap;

    private ResMgr()
    {
        _resLoader = new AddressableLoader();
        _genericMap = new Dictionary<string, MethodInfo>();
        
        // 图集有不同配置的时候使用
        // SpriteAtlasManager.atlasRequested -= RequestAtlas;
        // SpriteAtlasManager.atlasRequested += RequestAtlas;
    }

    private void RequestAtlas(string tag, Action<SpriteAtlas> callback)
    {
        Log.Info("要加载图集了：", tag);
        var b = LoadSync<SpriteAtlas>(tag);
        Timer.StartDelay(5000, i =>
        {
            callback(b);
        });
    }

    public T LoadSync<T>(string key)
    {
        return _resLoader.LoadSync<T>(key);
    }

    public void LoadAsync<T>(string key, Action<T> callback)
    {
        _resLoader.LoadAsync(key, callback);
    }
    
    public void LoadPrefab(string key, Action<GameObject> callback)
    {
        LoadAsync(key, callback);
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
                throw new GameException("不存在的类型" + type);
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
}