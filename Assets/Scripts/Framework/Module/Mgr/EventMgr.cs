/*--------------------------------------------------------------
 * File: EventCenter.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/30 17:34:10 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;

/// <summary>
/// 抛事件时，如果注册的回调参数结构、数量不一致都将不会触发;
/// </summary>
public class EventMgr : ManagerBase
{
    // Enum Dictionary<Enum, ...> 内部用 EqualityComparer<Enum>.Default 做比较，它的 GetHashCode 和 Equals 实现会把 enum 值 装箱为 object 再比较。
    // 用具体的enum类型 .Net做过优化，直接比较底层int值，无装箱
    //private readonly Dictionary<Enum, List<Delegate>> _eventMap = new Dictionary<Enum, List<Delegate>>();
    
    // 具体的enum只能业务层提供，框架层无法提供，故只能用int
    private readonly Dictionary<int, List<Delegate>> _eventMap = new Dictionary<int, List<Delegate>>();
    
    private static int GetKey<TEnum>(TEnum eventId) where TEnum : struct, Enum
    {
        // EqualityComparer<TEnum>.Default 对具体 enum 不装箱
        //return typeof(TEnum).GetHashCode() ^ EqualityComparer<TEnum>.Default.GetHashCode(eventId);
        
        // 暂定业务层只有一个TEnum，无须GetHashCode(),上述的GetHashCode还有理论上的哈希碰撞问题。
        return EqualityComparer<TEnum>.Default.GetHashCode(eventId);
    }

    public void Register<TEnum>(TEnum eventId, Action callback) where TEnum : struct, Enum
    {
        RegisterEvent(eventId, callback);
    }
    
    public void Register<TEnum, T>(TEnum eventId, Action<T> callback) where TEnum : struct, Enum
    {
        RegisterEvent(eventId, callback);
    }
    
    public void Register<TEnum, T1, T2>(TEnum eventId, Action<T1, T2> callback) where TEnum : struct, Enum
    {
        RegisterEvent(eventId, callback);
    }
    
    public void Register<TEnum, T1, T2, T3>(TEnum eventId, Action<T1, T2, T3> callback) where TEnum : struct, Enum
    {
        RegisterEvent(eventId, callback);
    }
    
    public void UnRegister<TEnum>(TEnum eventId, Action callback) where TEnum : struct, Enum
    {
        UnRegisterEvent(eventId, callback);
    }
    
    public void UnRegister<TEnum, T>(TEnum eventId, Action<T> callback) where TEnum : struct, Enum
    {
        UnRegisterEvent(eventId, callback);
    }
    
    public void UnRegister<TEnum, T1, T2>(TEnum eventId, Action<T1, T2> callback) where TEnum : struct, Enum
    {
        UnRegisterEvent(eventId, callback);
    }
    
    public void UnRegister<TEnum, T1, T2, T3>(TEnum eventId, Action<T1, T2, T3> callback) where TEnum : struct, Enum
    {
        UnRegisterEvent(eventId, callback);
    }

    public void UnRegisterAll<TEnum>(TEnum eventId) where TEnum : struct, Enum
    {
        var key = GetKey(eventId);
        _eventMap.Remove(key);
    }

    public void Clear()
    {
        _eventMap.Clear();
    }

    public void Send<TEnum>(TEnum eventId) where TEnum : struct, Enum
    {
        var key = GetKey(eventId);
        if (_eventMap.TryGetValue(key, out var list))
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                (list[i] as Action)?.Invoke();
            }     
        }
    }
    
    public void Send<TEnum, T>(TEnum eventId, T arg) where TEnum : struct, Enum
    {
        var key = GetKey(eventId);
        if (_eventMap.TryGetValue(key, out var list))
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                (list[i] as Action<T>)?.Invoke(arg);
            }         
        }
    }
    
    public void Send<TEnum, T1, T2>(TEnum eventId, T1 arg1, T2 arg2) where TEnum : struct, Enum
    {
        var key = GetKey(eventId);
        if (_eventMap.TryGetValue(key, out var list))
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                (list[i] as Action<T1, T2>)?.Invoke(arg1, arg2);
            }         
        }
    }
    
    public void Send<TEnum, T1, T2, T3>(TEnum eventId, T1 arg1, T2 arg2, T3 arg3) where TEnum : struct, Enum
    {
        var key = GetKey(eventId);
        if (_eventMap.TryGetValue(key, out var list))
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                (list[i] as Action<T1, T2, T3>)?.Invoke(arg1, arg2, arg3);
            }       
        }
    }

    private void RegisterEvent<TEnum>(TEnum eventId, Delegate callback) where TEnum : struct, Enum
    {
        int key = GetKey<TEnum>(eventId);
        if (!_eventMap.TryGetValue(key, out var list))
        {
            list = new List<Delegate>();
            _eventMap[key] = list;
        }

        if (list.Contains(callback))
        {
            Log.Warning("重复注册事件回调", eventId);
            return;
        }
        
        list.Add(callback);
    }

    private void UnRegisterEvent<TEnum>(TEnum eventId, Delegate callback) where TEnum : struct, Enum
    {
        int key = GetKey<TEnum>(eventId);
        if (_eventMap.TryGetValue(key, out var list))
        {
            var ret = list.Remove(callback);
            if (!ret)
            {
                Log.Error("正在注销未被监听的回调，请检查注册、注销是否为同一个方法", eventId);
            }
            else
            {
                if (list.Count == 0)
                {
                    _eventMap.Remove(key);
                }
            }
        }
        else
        {
            Log.Error("正在注销未被注册的事件", eventId);
        }
    }
}