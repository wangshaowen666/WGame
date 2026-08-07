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
        
        CoreMgr.Event.Register(GameEvent.ProcedureExitMain, RunProcedure);
        GameCamera.SetMainCamera(GameCamera.MapCameraName);
        GameMgr.UI.PanelOn(DPnlId.MainPanel);
    }

    public override void OnExit()
    {
        base.OnExit();
        CoreMgr.Event.UnRegister(GameEvent.ProcedureExitMain, RunProcedure);
    }

    private void RunProcedure()
    {
        _fsm.SetData(ProcedureKey.SceneName, GameConfig.BattleScene);
        ChangeTo<ProcedureChangeScene>();
    }
}