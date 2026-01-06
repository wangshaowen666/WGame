/*--------------------------------------------------------------
 * File: SettingPanel.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/06 14:09:22 
 *--------------------------------------------------------------
 */

using System;
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
        Destroy(gameObject);
    }

    private void ClickConfirm()
    {
        Destroy(gameObject);
    }
}