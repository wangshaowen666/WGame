/*--------------------------------------------------------------
 * File: ProcedureMain.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/06 15:58:20 
 *--------------------------------------------------------------
 */

using System;
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
        
        ScreenCtr.Instance.SetMainCamera(JITConfig.MapCamera);
        //PanelCtr.Instance.PanelOn("MainPanel");
    }

    private void FirstEnter()
    {
        _firstEnter = false;
        ScreenCtr.Instance.Init();
    }
}