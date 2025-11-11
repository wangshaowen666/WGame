/*--------------------------------------------------------------
 * File: Fsm.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/07 17:04:18 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Linq;

public class Fsm : IClass
{
    private readonly Dictionary<Type, FsmState> _states = new Dictionary<Type, FsmState>();
    // 序号比较会将字符串视为一系列的UTF-16代码单元进行精确的二进制比较,只严格检查每个字符的编码值是否完全一致,性能最高
    private Dictionary<string, Variable> _datas = new Dictionary<string, Variable>(StringComparer.Ordinal);
    private FsmState _curState;

    public FsmState CurState
    {
        get { return _curState; }
    }
    
    public void Init(params FsmState[] states)
    {
        if (states == null || states.Length == 0)
        {
            Log.Error("状态机拥有状体不可为空");
            return;
        }

        Init(states.ToList());
    }
    
    public void Init(List<FsmState> states)
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
                Log.Error("状态已经存在：", type.Name);
                return;
            }
            
            _states.Add(type, state);
            state.OnInit(this);
        }
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
        if (string.IsNullOrEmpty(name))
        {
            Log.Error("变量名不可为空");
            return null;
        }

        if (_datas.TryGetValue(name, out var variable))
        {
            return variable;
        }

        return null;
    }

    public void SetData<T>(string name, T value) where T : Variable
    {
        SetData(name, value as Variable);
    }

    public void SetData(string name, Variable value)
    {
        if (string.IsNullOrEmpty(name))
        {
            Log.Error("变量名不可为空");
            return;
        }

        Variable oldV = GetData(name);
        if (oldV != null)
        {
            ClassPoolFactory.Recycle(oldV);
        }
        
        _datas[name] = value;
    }
    
    public void Reset()
    {
        _curState = null;
        _datas.Clear();
        _states.Clear();
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