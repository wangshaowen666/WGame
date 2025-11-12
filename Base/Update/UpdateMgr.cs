/*--------------------------------------------------------------
 * File: GameFrameworkEntry.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/11 11:29:18 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 规避GF框架很多空的Update调用，采用按需注册的形式
/// </summary>
public static class UpdateMgr
{
    private static readonly List<IUpdateable> _updateMaps = new List<IUpdateable>();
    // Update时可能触发注册和移除，放到缓存中，在下一帧的遍历中才生效
    private static readonly List<IUpdateable> _addCaches = new List<IUpdateable>();
    private static readonly List<IUpdateable> _rmvCaches = new List<IUpdateable>();

    private static bool _addItem;
    private static bool _rmvItem;

    public static void RegisterUpdate(IUpdateable updateable)
    {
        _addItem = true;
        for (int i = 0; i < _addCaches.Count; i++)
        {
            if (updateable.Priority > _addCaches[i].Priority)
            {
                _addCaches.Insert(i, updateable);
                return;
            }
        }
        
        _addCaches.Add(updateable);
    }

    public static void UnRegisterUpdate(IUpdateable updateable)
    {
        _rmvItem = true;
        for (int i = 0; i < _rmvCaches.Count; i++)
        {
            if (updateable.Priority > _rmvCaches[i].Priority)
            {
                _rmvCaches.Insert(i, updateable);
                return;
            }
        }
        
        _rmvCaches.Add(updateable);
    }
    
    public static void Update(float deltaTime, float realDeltaTime)
    {
        foreach (var u in _updateMaps)
        {
            u.Update(deltaTime, realDeltaTime);
        }

        // 处理移除和添加逻辑
        if (_addItem || _rmvItem)
        {
            DealCache();
            _addItem = false;
            _rmvItem = false;
        }
    }

    private static void DealCache()
    {
        for (int i = 0; i < _updateMaps.Count; i++)
        {
            if (_addItem && _addCaches.Count > 0)
            {
                if (_updateMaps[i].Priority < _addCaches[0].Priority)
                {
                    _updateMaps.Insert(i, _addCaches[0]);
                    _addCaches.RemoveAt(0);
                    i++;
                }
            }
            
            // 先减后加，有可能0号元素移除，下标变成-1，加的时候报错
            if (_rmvItem && _rmvCaches.Count > 0)
            {
                if (_updateMaps[i] == _rmvCaches[0])
                {
                    _rmvCaches.RemoveAt(0);
                    _updateMaps.RemoveAt(i);
                    i--;
                }
            }
        }

        if (_addCaches.Count > 0)
        {
            foreach (var updateable in _addCaches)
            {
                _updateMaps.Add(updateable);
            }
            _addCaches.Clear();
        }

        if (_rmvCaches.Count > 0)
        {
            Log.Error("重复移除或者尝试移除未注册的Updateable");
            _rmvCaches.Clear();
        }
    }
}