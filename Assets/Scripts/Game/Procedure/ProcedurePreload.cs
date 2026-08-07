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
        // todo 预加载配置表、图集、字体等
        
        // 加载配置表
        GameMgr.DataTable.LoadTable();
        
        // 启动lua
        // CoreMgr.Lua.InitLuaEnv();
        
        await UniTask.Yield();
        _fsm.RemoveData(LaunchConfig.LoginPanel);
        
        // 登陆进主界面这次，没走ProcedureChangeScene流程，那里还包含了过场界面展示、资源卸载等逻辑，首次不需要
        CoreMgr.Res.LoadSceneAsync(GameConfig.MainScene, null, OnSceneComplete);
    }

    private void OnSceneComplete()
    {
        Object.Destroy(_loginPanel.gameObject);
        ChangeTo<ProcedureMain>();
    }
}