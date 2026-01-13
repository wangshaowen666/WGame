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
    public Button btnSetting;

    private void Awake()
    {
        Log.Info("进入主界面");
        btnSetting.onClick.AddListener(OnClickSetting);
    }

    private void OnClickSetting()
    {
        PanelCtr.Instance.PanelOn(DPnlId.SettingPanel);
    }
}