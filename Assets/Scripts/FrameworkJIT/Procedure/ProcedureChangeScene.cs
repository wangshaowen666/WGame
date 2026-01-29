/*--------------------------------------------------------------
 * File: ProcedureChangeScene.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/22 18:26:05 
 *--------------------------------------------------------------
 */

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
        var nm = _fsm.GetObj<string>("sceneNm");
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
                Procedure.Instance.RunProcedure<ProcedureMain>();
                break;
            
            case "Battle":
                Procedure.Instance.RunProcedure<ProcedureBattle>();
                break;
            
            default:
                Log.Error("未实现的场景：", nm);
                return;
        }
    }
}