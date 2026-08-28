/*--------------------------------------------------------------
 * File: ProcedurePreload.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/13 13:36:48 
 *--------------------------------------------------------------
 */

using Cysharp.Threading.Tasks;
using UnityEngine;

public static class ProcedureKey
{
    public const string SceneName = "sceneNm";
}

public class ProcedurePreload : ProcedureBase
{
    private LoginPanel _loginPanel;
    
    public override void OnEnter()
    {
        base.OnEnter();
        AsyncRun().Forget();
    }
    
    private async UniTaskVoid AsyncRun()
    {
        _loginPanel = _fsm.GetData<LoginPanel>(LaunchConfig.LoginPanel);
        _loginPanel.SetTip("编译着色器中...", 0.9f);

        // 加载配置表
        GameMgr.DataTable.LoadTable();
        
        await UniTask.Yield();
        _fsm.RemoveData(LaunchConfig.LoginPanel);
        Object.Destroy(_loginPanel.gameObject);
        
        //_fsm.SetData(ProcedureKey.SceneName, GameConfig.MainScene);
        
        _fsm.SetData(ProcedureKey.SceneName, GameConfig.TestScene);
        
        ChangeTo<ProcedureChangeScene>();
    }
}