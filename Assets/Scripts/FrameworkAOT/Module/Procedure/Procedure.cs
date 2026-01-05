/*--------------------------------------------------------------
 * File: Procedure.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 15:42:27 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 基于状态机的游戏生命周期管理流程
/// </summary>
public class Procedure : Singleton<Procedure>, IUpdateable
{
    private Fsm _fsm;

    private Procedure()
    {
        // 原本是通过反射自动收集所有流程，用华佗热更拆分程序集后，反射拿不到热更程序集中的流程
        var procedures = new List<ProcedureBase>
        {
            new ProcedureLaunch(),
            new ProcedureVersionCheck(),
            new ProcedureResCheck_AA(),
            new ProcedureLoadDll()
        };
        _fsm = Fsm.Create(procedures);
        
        UpdateMgr.RegisterUpdate(this);
    }
    
    public void AddProcedure(ProcedureBase procedure)
    {
        _fsm.AddState(procedure);
    }

    public void RunProcedure<T>() where T : ProcedureBase
    {
        _fsm.ChangeState<T>();
    }
    
    public void Update(float deltaTime, float realDeltaTime)
    {
        if (_fsm.CurState != null)
        {
            _fsm.CurState.OnUpdate(deltaTime, realDeltaTime);
        }
    }

    /// <summary>
    /// 通过反射获取业务逻辑的所有流程
    /// </summary>
    /// <returns></returns>
    private List<ProcedureBase> GetAllProcedures()
    {
        List<ProcedureBase> procedures = new List<ProcedureBase>();
        
        // 获取所有程序集
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // 如果某些类型无法加载，使用已成功加载的类型继续
                types = e.Types;
            }

            foreach (var type in types)
            {
                if (type == null)
                    continue;

                if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(ProcedureBase)))
                {
                    try
                    {
                        ProcedureBase procedure = Activator.CreateInstance(type) as ProcedureBase;
                        procedures.Add(procedure);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"创建流程实例失败：{type.FullName}, 错误：{e.Message}");
                    }
                }
            }
        }
        
        return procedures;
    }
}