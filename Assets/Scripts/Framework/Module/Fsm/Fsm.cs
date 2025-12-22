/*--------------------------------------------------------------
 * File: Fsm.cs
 * Author: Wang ShaoWen
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
    private Dictionary<string, Variable> _datas = new Dictionary<string, Variable>(StringComparer.Ordinal);
    // GameObject以及相关属性、类在这里缓存，设置主要用来避免值类型的装拆箱
    private Dictionary<string, GameObject> _objs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    private FsmState _curState;

    public FsmState CurState
    {
        get { return _curState; }
    }

    public static Fsm Create<T>(params T[] states) where T : FsmState
    {
        Fsm fsm = ClassFactory.Get<Fsm>();
        fsm.Init(states.ToList());
        return fsm;
    }

    public static Fsm Create<T>(List<T> states) where T : FsmState
    {
        Fsm fsm = ClassFactory.Get<Fsm>();
        fsm.Init(states.ToList());
        return fsm;
    }

    public void ChangeState<T>() where T : FsmState
    {
        ChangeState(typeof(T));
    }

    public void ChangeState(Type type)
    {
        var state = GetState(type);
        if (state == null)
        {
            Log.Error("无效的状态类型", type.Name);
            return;
        }
        
        if (_curState != null)
        {
            _curState.OnExit();
        }
        
        _curState = state;
        state.OnEnter();
    }

    public T GetData<T>(string name) where T : Variable
    {
        return GetData(name) as T;
    }

    public Variable GetData(string name)
    {
        return _datas.GetValueOrDefault(name);
    }

    public GameObject GetObj(string name)
    {
        return _objs.GetValueOrDefault(name);
    }

    public void SetData<T>(string name, T value) where T : Variable
    {
        SetData(name, value as Variable);
    }

    public void SetData(string name, Variable value)
    {
        Variable oldV = GetData(name);
        if (oldV != null)
        {
            ClassFactory.Recycle(oldV);
        }
        
        _datas[name] = value;
    }

    public void SetObj(string name, GameObject obj)
    {
        _objs[name] = obj;
    }
    
    public void Reset()
    {
        _curState = null;
        _datas.Clear();
        _objs.Clear();
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

        if (!_states.TryGetValue(type, out FsmState state))
        {
            return null;
        }
        
        return state;
    }
}