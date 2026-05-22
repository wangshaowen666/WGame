/*--------------------------------------------------------------
 * File: GameStart.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/04 18:41:15 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using HybridCLR;
using UnityEngine;

/// <summary>
/// 可热更的C#代码启动入口，热更后通过反射调用的
/// </summary>
public class GameLaunch 
{
    public static void StartGame()
    {
        //Application.targetFrameRate = 60;
        ReplenishMeta();
        InitGameProcedure();
      
        ProcedureMgr.RunProcedure<ProcedurePreload>();
    }

    /// <summary>
    /// 添加主流程，最初是通过反射无需手动添加，拆分程序集后需手动注册
    /// </summary>
    private static void InitGameProcedure()
    {
        ProcedureMgr.AddProcedure(new ProcedureChangeScene());
        ProcedureMgr.AddProcedure(new ProcedurePreload());
        ProcedureMgr.AddProcedure(new ProcedureMain());
        ProcedureMgr.AddProcedure(new ProcedureBattle());
    }
    
    /// <summary>
    /// 补充元数据
    /// </summary>
    private static void ReplenishMeta()
    {
        List<string> aotDllList = new List<string>
        {
            "mscorlib.dll",
            "UniTask.dll",
            "Unity.Netcode.Runtime.dll",
        };

        foreach (var aotDllName in aotDllList)
        {
            byte[] dllBytes = FrameworkMgr.Res.LoadSync<TextAsset>(aotDllName).bytes;
            RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
            Log.Info("补充元数据dll:", aotDllName);
        }
    }
}