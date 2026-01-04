/*--------------------------------------------------------------
 * File: ProcedurePreload.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/13 13:36:48 
 *--------------------------------------------------------------
 */

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

public class ProcedurePreload : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        Log.Info("进入预加载流程");
        
        AsyncRun().Forget();
    }
    
    private async UniTaskVoid AsyncRun()
    {
        var panel = _fsm.GetObj<LoginPanel>("loginPanel");
        panel.SetTip("编译着色器中...", 0.9f);
        // todo 预加载配置表、图集、字体等

        await UniTask.Delay(300);
        
        _fsm.SetObj("sceneNm", "Main");
        Procedure.Instance.RunProcedure<ProcedureChangeScene>();
    }
}