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
    public override void OnEnter()
    {
        base.OnEnter();
        if (!_fsm.GetData<bool>("isLoginFinish"))
            FirstEnter();
        
        CoreMgr.Event.Register(GameEvent.ProcedureExitMain, RunProcedure);
        GameCamera.SetMainCamera(GameCamera.MapCameraName);
        GameMgr.UI.PanelOn(DPnlId.MainPanel);
    }

    public override void OnExit()
    {
        base.OnExit();
        CoreMgr.Event.UnRegister(GameEvent.ProcedureExitMain, RunProcedure);
    }

    private void FirstEnter()
    {
        _fsm.SetData("isLoginFinish", true);
        
        //FrameworkMgr.Screen.Init();
        //GameMgr.UI.CreateUIRoot();
    }

    private void RunProcedure()
    {
        _fsm.SetData("sceneNm", "Battle");
        ChangeTo<ProcedureChangeScene>();
    }
}