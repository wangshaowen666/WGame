/*--------------------------------------------------------------
 * File: ProcedurePreload.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/13 13:36:48 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class ProcedurePreload : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        Log.Info("进入预加载流程");
        
        var panel = _fsm.GetObj<LoginPanel>("loginPanel");
        panel.SetTip("编译着色器中...", 0.9f);
        
        
        _fsm.SetObj("sceneNm", "Main");
        Procedure.Instance.RunProcedure<ProcedureChangeScene>();
    }
}