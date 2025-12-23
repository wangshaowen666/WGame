/*--------------------------------------------------------------
 * File: ProcedureChangeScene.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/22 18:26:05 
 *--------------------------------------------------------------
 */

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ProcedureChangeScene : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        
        AsyncRun().Forget();
    }

    private async UniTaskVoid AsyncRun()
    {
        var nm = _fsm.GetObj<string>("sceneNm");
        await SceneCtr.Instance.LoadScene(nm);
        
    }
}