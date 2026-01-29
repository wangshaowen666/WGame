/*--------------------------------------------------------------
 * File: GameStart.cs
 * Author: Wang ShaoWen
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
        Application.targetFrameRate = 60;
        ReplenishMeta();
        InitGameProcedure();
        BattleMgr.Instance.Init();
        Procedure.Instance.RunProcedure<ProcedurePreload>();
    }

    private static void InitGameProcedure()
    {
        Procedure.Instance.AddProcedure(new ProcedureChangeScene());
        Procedure.Instance.AddProcedure(new ProcedurePreload());
        Procedure.Instance.AddProcedure(new ProcedureMain());
        Procedure.Instance.AddProcedure(new ProcedureBattle());
    }
    
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
            byte[] dllBytes = ResMgr.Instance.LoadSync<TextAsset>(aotDllName).bytes;
            RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
            Log.Info("补充元数据dll:", aotDllName);
        }
    }
}