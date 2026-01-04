/*--------------------------------------------------------------
 * File: MainPanel.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/31 18:43:16 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : UIPanelBase
{
    public Button btnSetting;

    private void Awake()
    {
        btnSetting.onClick.AddListener(OnClickSetting);
    }

    private void OnClickSetting()
    {
        PanelCtr.Instance.PanelOn("SettingPanel");
    }
}