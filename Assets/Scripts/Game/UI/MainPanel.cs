/*--------------------------------------------------------------
 * File: MainPanel.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/31 18:43:16 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;
using UnityEngine.U2D;
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
        //ResMgr.Instance.LoadSync<SpriteAtlas>("atlas_acti7086_main_s6091_hd");
        //PanelCtr.Instance.PanelOn("SettingPanel");
    }
}