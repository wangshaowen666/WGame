/*--------------------------------------------------------------
 * File: ProcedureMain.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/06 15:58:20 
 *--------------------------------------------------------------
 */

using System;
using cfg;
using UnityEngine;

public class ProcedureMain : ProcedureBase
{
    // 从登陆界面首次进入主界面流程
    private bool _firstEnter = true;

    public override void OnEnter()
    {
        base.OnEnter();
        if (_firstEnter)
            FirstEnter();
        
        FrameworkMgr.Event.Register(GameEvent.ProcedureExitMain, RunProcedure);
        FrameworkMgr.Screen.SetMainCamera(FrameworkConfig.MapCamera);
        GameMgr.UI.PanelOn(DPnlId.MainPanel);
    }

    public override void OnExit()
    {
        base.OnExit();
        FrameworkMgr.Event.UnRegister(GameEvent.ProcedureExitMain, RunProcedure);
    }

    private void FirstEnter()
    {
        _firstEnter = false;
        
        FrameworkMgr.Screen.Init();
        GameMgr.UI.CreateUIRoot();
    }

    private void RunProcedure()
    {
        _fsm.SetData("sceneNm", "Battle");
        ProcedureMgr.RunProcedure<ProcedureChangeScene>();
    }
}