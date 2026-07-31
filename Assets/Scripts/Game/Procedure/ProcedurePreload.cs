/*--------------------------------------------------------------
 * File: ProcedurePreload.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
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
        var panel = _fsm.GetData<LoginPanel>("loginPanel");
        panel.SetTip("编译着色器中...", 0.9f);
        // todo 预加载配置表、图集、字体等
        
        // 加载配置表
        GameMgr.DataTable.LoadTable();
        
        // 启动lua
        FrameworkMgr.Lua.InitLuaEnv();
        
        await UniTask.Delay(300);
        
        _fsm.RemoveData("loginPanel");
        _fsm.SetData("sceneNm", "Main");
        ChangeTo<ProcedureChangeScene>();
    }
}