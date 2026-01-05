/*--------------------------------------------------------------
 * File: GameStart.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/04 18:41:15 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

/// <summary>
/// 可热更的C#代码启动入口，热更后通过反射调用的
/// </summary>
public class GameLaunch 
{
    public static void StartGame()
    {
        InitGameProcedure();
        Procedure.Instance.RunProcedure<ProcedurePreload>();
    }

    private static void InitGameProcedure()
    {
        Procedure.Instance.AddProcedure(new ProcedureChangeScene());
        Procedure.Instance.AddProcedure(new ProcedurePreload());
    }
}