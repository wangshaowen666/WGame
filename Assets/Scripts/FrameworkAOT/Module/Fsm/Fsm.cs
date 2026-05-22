/*--------------------------------------------------------------
 * File: Fsm.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/07 17:04:18 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Fsm : IResetable
{
    private readonly Dictionary<Type, FsmState> _states = new Dictionary<Type, FsmState>();
    // 序号比较会将字符串视为一系列的UTF-16代码单元进行精确的二进制比较,只严格检查每个字符的编码值是否完全一致,性能最高
    // 如果存储的是值类型，用Variable
    private Dictionary<string, object> _datas = new Dictionary<string, object>(StringComparer.Ordinal);
    private FsmState _curState;

    public FsmState CurState => _curState;

    public static Fsm Create<T>(params T[] states) where T : FsmState
    {
        Fsm fsm = ClassPool.Get<Fsm>();
        fsm.Init(states.ToList());
        return fsm;
    }

    public static Fsm Create<T>(List<T> states) where T : FsmState
    {
        Fsm fsm = ClassPool.Get<Fsm>();
        fsm.Init(states.ToList());
        return fsm;
    }

    public void ChangeState<T>() where T : FsmState
    {
        var state = GetState(typeof(T));
        if (state == null)
        {
            Log.Error("无效的状态类型", typeof(T).Name);
            return;
        }
        
        if (_curState != null)
        {
            _curState.OnExit();
        }
        
        _curState = state;
        state.OnEnter();
    }

    public void AddState<T>(T state) where T : FsmState
    {
        Type type = state.GetType();
        if (!_states.TryAdd(type, state))
        {
            Log.Error("状态已经存在, 不可重复添加：", type.Name);
            return;
        }

        state.OnInit(this);
    }

    public T GetData<T>(string name) where T : class
    {
        return _datas[name] as T;
    }

    public void SetData<T>(string name, T value) where T : class
    {
        if (_datas.ContainsKey(name))
        {
            var oldTp = _datas[name].GetType();
            if (oldTp != typeof(T))
                Log.Warning("状态机数据key已存在，本次写入类型不匹配 会覆盖旧类型结果，key:", name, "原始类型:", oldTp, "当前设置类型：", typeof(T).Name);

            if (oldTp.IsSubclassOf(typeof(IResetable)))
                ClassPool.Recycle(_datas[name] as IResetable);
        }
        
        _datas[name] = value;
    }

    public void RemoveData(string name)
    {
        _datas.Remove(name, out object obj);
        if (obj.GetType().IsSubclassOf(typeof(IResetable)))
            ClassPool.Recycle(obj as IResetable);
    }
    
    public void Reset()
    {
        _curState = null;
        _datas.Clear();
        _states.Clear();
    }
    
    private void Init<T>(List<T> states) where T : FsmState
    {
        if (states == null || states.Count == 0)
        {
            Log.Error("状态机拥有状体不可为空");
            return;
        }
        
        foreach (var state in states)
        {
            if (state == null)
            {
                Log.Error("无效的状态");
                return;
            }
            
            Type type = state.GetType();
            if (_states.ContainsKey(type))
            {
                Log.Error("状态已经存在, 不可重复添加：", type.Name);
                return;
            }
            
            _states.Add(type, state);
            state.OnInit(this);
        }
    }

    private FsmState GetState(Type type)
    {
        if (type == null)
        {
            return null;
        }

        return _states.GetValueOrDefault(type);
    }
}