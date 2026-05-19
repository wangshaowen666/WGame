/*--------------------------------------------------------------
 * File: ProcedureChangeScene.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/22 18:26:05 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class ProcedureChangeScene : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        
        // todo 释放资源等
        
        AsyncRun().Forget();
    }

    private async UniTaskVoid AsyncRun()
    {
        var nm = _fsm.GetData<string>("sceneNm");

        _fsm.SetData("hah", (VarInt)1);
        if (string.IsNullOrEmpty(nm))
        {
            Log.Error("场景名未赋值");
            return;
        }
        else
        {
            Log.Info("场景名：", nm);
        }
       
        await SceneCtr.Instance.LoadScene(nm);
        switch (nm)
        {
            case "Main":
                ProcedureMgr.Instance.RunProcedure<ProcedureMain>();
                break;
            
            case "Battle":
            case "Network":
                ProcedureMgr.Instance.RunProcedure<ProcedureBattle>();
                break;
            
            default:
                Log.Error("未实现的场景：", nm);
                return;
        }
    }
}