/*--------------------------------------------------------------
 * File: SettingPanel.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/06 14:09:22 
 *--------------------------------------------------------------
 */

using System;
using Coffee.UIExtensions;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : UIPanelBase
{
    public Button btnConfirm;
    public Button btnCancel;

    private void Awake()
    {
        btnConfirm.onClick.AddListener(ClickConfirm);
        btnCancel.onClick.AddListener(ClickCancel);
    }

    private void ClickCancel()
    {
        GameMgr.UI.PanelOff(this);
    }

    private void ClickConfirm()
    {
        GameMgr.UI.PanelOff(this);
    }
}