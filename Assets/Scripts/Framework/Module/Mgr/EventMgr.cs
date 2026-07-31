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
    private readonly Dictionary<Enum, List<Delegate>> _eventMap = new Dictionary<Enum, List<Delegate>>();

    public void Register(Enum eventId, Action callback)
    {
        RegisterEvent(eventId, callback);
    }
    
    public void Register<T>(Enum eventId, Action<T> callback)
    {
        RegisterEvent(eventId, callback);
    }
    
    public void Register<T1, T2>(Enum eventId, Action<T1, T2> callback)
    {
        RegisterEvent(eventId, callback);
    }
    
    public void Register<T1, T2, T3>(Enum eventId, Action<T1, T2, T3> callback)
    {
        RegisterEvent(eventId, callback);
    }
    
    public void UnRegister(Enum eventId, Action callback)
    {
        UnRegisterEvent(eventId, callback);
    }
    
    public void UnRegister<T>(Enum eventId, Action<T> callback)
    {
        UnRegisterEvent(eventId, callback);
    }
    
    public void UnRegister<T1, T2>(Enum eventId, Action<T1, T2> callback)
    {
        UnRegisterEvent(eventId, callback);
    }
    
    public void UnRegister<T1, T2, T3>(Enum eventId, Action<T1, T2, T3> callback)
    {
        UnRegisterEvent(eventId, callback);
    }

    public void UnRegisterAll(Enum eventId)
    {
        _eventMap.Remove(eventId);
    }

    public void Clear()
    {
        _eventMap.Clear();
    }

    public void Send(Enum eventId)
    {
        if (_eventMap.TryGetValue(eventId, out var list))
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                (list[i] as Action)?.Invoke();
            }     
        }
    }
    
    public void Send<T>(Enum eventId, T arg)
    {
        if (_eventMap.TryGetValue(eventId, out var list))
        {
            foreach (var handler in list)
            {
                (handler as Action<T>)?.Invoke(arg);
            }       
        }
    }
    
    public void Send<T1, T2>(Enum eventId, T1 arg1, T2 arg2)
    {
        if (_eventMap.TryGetValue(eventId, out var list))
        {
            foreach (var handler in list)
            {
                (handler as Action<T1, T2>)?.Invoke(arg1, arg2);
            }       
        }
    }
    
    public void Send<T1, T2, T3>(Enum eventId, T1 arg1, T2 arg2, T3 arg3)
    {
        if (_eventMap.TryGetValue(eventId, out var list))
        {
            foreach (var handler in list)
            {
                (handler as Action<T1, T2, T3>)?.Invoke(arg1, arg2, arg3);
            }       
        }
    }

    private void RegisterEvent(Enum eventId, Delegate callback)
    {
        if (!_eventMap.TryGetValue(eventId, out var list))
        {
            list = new List<Delegate>();
            _eventMap[eventId] = list;
        }
        
        list.Add(callback);
    }

    private void UnRegisterEvent(Enum eventId, Delegate callback)
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