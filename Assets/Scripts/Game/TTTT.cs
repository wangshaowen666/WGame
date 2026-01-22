/*--------------------------------------------------------------
 * File: TTTT.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 18:09:09 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TTTT : MonoBehaviour
{
    private enum MyEnum
    {
        OnLevelUp = 1,
    }
    
    private string str;
    private List<string> hashNmd = new List<string>();
    
    private void Start()
    {
        //Procedure.Instance.RunProcedure<ProcedureVersionCheck>();
    }

    [ContextMenu("打开界面")]
    void Execute3()
    {
        UIMgr.Instance.PanelOn(cfg.DPnlId.SettingPanel);
    }
    
    [ContextMenu("关闭界面")]
    void Execute4()
    {
        UIMgr.Instance.PanelOff(cfg.DPnlId.SettingPanel);
    }
    
    
    [ContextMenu("执行")]
    void Execute()
    {
        ResMgr.Instance.LoadAsync<TextAsset>("TTT.lua");
    }
    
    [ContextMenu("执行2")]
    void Execute2()
    {
        ResMgr.Instance.LoadAsync<TextAsset>("Main.lua");
    }
    
    [ContextMenu("卸载1")]
    void Unload1()
    {
        ResMgr.Instance.Unload("TTT.lua");
    }
    
    [ContextMenu("卸载2")]
    void Unload2()
    {
        ResMgr.Instance.Unload("Main.lua");
    }

    void Update()
    {
        for (int i = 0; i < 1000; i++)
        {
            var t = new string[i];
        }
    }
}