/*--------------------------------------------------------------
 * File: ProcedureLaunch.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 16:25:31 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class ProcedureLaunch : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        
        ScreenCtr.Instance.Init();
        
        var panel = PanelCtr.Instance.LoadLoginPanel();
        panel.SetTip("游戏启动中...", 0.1f);
        
        _fsm.SetObj("loginPanel", panel);
        Procedure.Instance.RunProcedure<ProcedureVersionCheck>();
    }
}