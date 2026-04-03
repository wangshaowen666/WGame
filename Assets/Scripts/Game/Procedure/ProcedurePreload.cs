/*--------------------------------------------------------------
 * File: ProcedurePreload.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/13 13:36:48 
 *--------------------------------------------------------------
 */

using Cysharp.Threading.Tasks;

public class ProcedurePreload : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        AsyncRun().Forget();
    }
    
    private async UniTaskVoid AsyncRun()
    {
        var panel = _fsm.GetObj<LoginPanel>("loginPanel");
        panel.SetTip("编译着色器中...", 0.9f);
        // todo 预加载配置表、图集、字体等
        
        // 加载配置表
        DataTableMgr.Instance.LoadTable();
        
        // 启动lua
        LuaCtr.Instance.InitLuaEnv();
        
        await UniTask.Delay(300);
        
        _fsm.RemoveObj("loginPanel");
        _fsm.SetObj("sceneNm", "Main");
        ProcedureMgr.Instance.RunProcedure<ProcedureChangeScene>();
    }
}