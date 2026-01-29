/*--------------------------------------------------------------
 * File: ProcedureMain.cs
 * Author: Wang ShaoWen
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
        
        EventMgr.Instance.Register(GameEvent.ClickGoBattle, OnGoBattle);
        
        ScreenCtr.Instance.SetMainCamera(JITConfig.MapCamera);
        UIMgr.Instance.PanelOn(DPnlId.MainPanel);
    }

    public override void OnExit()
    {
        base.OnExit();
        EventMgr.Instance.UnRegister(GameEvent.ClickGoBattle, OnGoBattle);
    }

    private void FirstEnter()
    {
        _firstEnter = false;
        ScreenCtr.Instance.Init();
    }

    private void OnGoBattle()
    {
        _fsm.SetObj("sceneNm", "Network");
        Procedure.Instance.RunProcedure<ProcedureChangeScene>();
    }
}