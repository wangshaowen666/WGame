/*--------------------------------------------------------------
 * File: Procedure.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/12 15:42:27 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;

/// <summary>
/// 基于状态机的游戏生命周期管理流程
/// </summary>
public static class ProcedureMgr
{
    // 登陆流程结束的标识
    public static bool LoginFinish { get; set; }

    private static Fsm s_fsm;
    static ProcedureMgr()
    {
        // 原本是通过反射自动收集所有流程，用华佗热更拆分程序集后，反射拿不到热更程序集中的流程
        var procedures = new List<ProcedureBase>
        {
            new ProcedureLaunch(),
            new ProcedureVersionCheck(),
            new ProcedureResCheck_AA(),
            new ProcedureLoadDll()
        };
        s_fsm = Fsm.Create(procedures);
    }
    
    public static void AddProcedure(ProcedureBase procedure)
    {
        s_fsm.AddState(procedure);
    }

    public static void RunProcedure<T>() where T : ProcedureBase
    {
        s_fsm.ChangeState<T>();
    }

    // 通过反射获取业务逻辑的所有流程
    // private static List<ProcedureBase> GetAllProcedures()
    // {
    //     List<ProcedureBase> procedures = new List<ProcedureBase>();
    //     
    //     // 获取所有程序集
    //     Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
    //     foreach (var assembly in assemblies)
    //     {
    //         Type[] types;
    //         try
    //         {
    //             types = assembly.GetTypes();
    //         }
    //         catch (ReflectionTypeLoadException e)
    //         {
    //             // 如果某些类型无法加载，使用已成功加载的类型继续
    //             types = e.Types;
    //         }
    //
    //         foreach (var type in types)
    //         {
    //             if (type == null)
    //                 continue;
    //
    //             if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(ProcedureBase)))
    //             {
    //                 try
    //                 {
    //                     ProcedureBase procedure = Activator.CreateInstance(type) as ProcedureBase;
    //                     procedures.Add(procedure);
    //                 }
    //                 catch (Exception e)
    //                 {
    //                     Log.Error($"创建流程实例失败：{type.FullName}, 错误：{e.Message}");
    //                 }
    //             }
    //         }
    //     }
    //     
    //     return procedures;
    // }
}