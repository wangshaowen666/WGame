/*--------------------------------------------------------------
 * File: ProcedureChangeScene.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/22 18:26:05 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using cfg;
using Cysharp.Threading.Tasks;

public class ProcedureChangeScene : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        
        // todo 释放资源等
        if (ProcedureMgr.LoginFinish)
            GameMgr.UI.PanelOn(DPnlId.LoadingPanel);
        
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
       
        await UniTask.Delay(3000);
        await FrameworkMgr.Scene.LoadScene(nm);

        if (ProcedureMgr.LoginFinish)
            GameMgr.UI.PanelOff(DPnlId.LoadingPanel);
        
        switch (nm)
        {
            case "Main":
                ProcedureMgr.RunProcedure<ProcedureMain>();
                break;
            
            case "Battle":
            case "Network":
                ProcedureMgr.RunProcedure<ProcedureBattle>();
                break;
            
            default:
                Log.Error("未实现的场景：", nm);
                return;
        }
    }
}