/*--------------------------------------------------------------
 * File: MainPanel.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/31 18:43:16 
 *--------------------------------------------------------------
 */

using System;
using cfg;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class MainPanel : UIPanelBase
{
    public Button btnStart;
    public Button btnSetting;

    public override void OnInit(DUIPanel cfg)
    {
        base.OnInit(cfg);
        Log.Info("进入主界面");
        btnSetting.onClick.AddListener(OnClickSetting);
        btnStart.onClick.AddListener(OnClickStart);
    }

    public override void OnOpen(object userData = null)
    {
        base.OnOpen(userData);
    }

    private void OnClickSetting()
    {
        UIMgr.Instance.PanelOn(DPnlId.SettingPanel);
    }

    private void OnClickStart()
    {
        EventMgr.Instance.Send(GameEvent.ProcedureExitMain);
        UIMgr.Instance.PanelOff(this);
    }
}