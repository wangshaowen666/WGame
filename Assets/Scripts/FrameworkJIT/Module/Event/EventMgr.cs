/*--------------------------------------------------------------
 * File: EventCenter.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/30 17:34:10 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 抛事件时，如果注册的回调参数结构、数量不一致都将不会触发;
/// 用范型存在的问题：后续事件需要新增、修改参数数量或类型等都会影响到旧方法，需要全部修改；
/// 如果用EventArg专门定义相关类，只需要在参数类和新的回调中处理即可。
/// </summary>
public class EventMgr : Singleton<EventMgr>
{
    private readonly Dictionary<GameEvent, List<Delegate>> _eventMap = new Dictionary<GameEvent, List<Delegate>>();

    private EventMgr()
    {
    }

    public void Register(GameEvent eventId, Action callback)
    {
        RegisterEvent(eventId, callback);
    }
    
    public void Register<T>(GameEvent eventId, Action<T> callback)
    {
        RegisterEvent(eventId, callback);
    }
    
    public void Register<T1, T2>(GameEvent eventId, Action<T1, T2> callback)
    {
        RegisterEvent(eventId, callback);
    }
    
    public void Register<T1, T2, T3>(GameEvent eventId, Action<T1, T2, T3> callback)
    {
        RegisterEvent(eventId, callback);
    }
    
    public void UnRegister(GameEvent eventId, Action callback)
    {
        UnRegisterEvent(eventId, callback);
    }
    
    public void UnRegister<T>(GameEvent eventId, Action<T> callback)
    {
        UnRegisterEvent(eventId, callback);
    }
    
    public void UnRegister<T1, T2>(GameEvent eventId, Action<T1, T2> callback)
    {
        UnRegisterEvent(eventId, callback);
    }
    
    public void UnRegister<T1, T2, T3>(GameEvent eventId, Action<T1, T2, T3> callback)
    {
        UnRegisterEvent(eventId, callback);
    }

    public void UnRegisterAll(GameEvent eventId)
    {
        _eventMap.Remove(eventId);
    }

    public void Clear()
    {
        _eventMap.Clear();
    }

    public void Send(GameEvent eventId)
    {
        if (_eventMap.TryGetValue(eventId, out var list))
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                (list[i] as Action)?.Invoke();
            }     
        }
    }
    
    public void Send<T>(GameEvent eventId, T arg)
    {
        if (_eventMap.TryGetValue(eventId, out var list))
        {
            foreach (var handler in list)
            {
                (handler as Action<T>)?.Invoke(arg);
            }       
        }
    }
    
    public void Send<T1, T2>(GameEvent eventId, T1 arg1, T2 arg2)
    {
        if (_eventMap.TryGetValue(eventId, out var list))
        {
            foreach (var handler in list)
            {
                (handler as Action<T1, T2>)?.Invoke(arg1, arg2);
            }       
        }
    }
    
    public void Send<T1, T2, T3>(GameEvent eventId, T1 arg1, T2 arg2, T3 arg3)
    {
        if (_eventMap.TryGetValue(eventId, out var list))
        {
            foreach (var handler in list)
            {
                (handler as Action<T1, T2, T3>)?.Invoke(arg1, arg2, arg3);
            }       
        }
    }

    private void RegisterEvent(GameEvent eventId, Delegate callback)
    {
        if (!_eventMap.TryGetValue(eventId, out var list))
        {
            list = new List<Delegate>();
            _eventMap[eventId] = list;
        }
        
        list.Add(callback);
    }

    private void UnRegisterEvent(GameEvent eventId, Delegate callback)
    {
        if (_eventMap.TryGetValue(eventId, out var list))
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
                    _eventMap.Remove(eventId);
                }
            }
        }
        else
        {
            Log.Error("正在注销未被注册的事件", eventId);
        }
    }
}