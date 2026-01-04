/*--------------------------------------------------------------
 * File: ProcedureChangeScene.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/22 18:26:05 
 *--------------------------------------------------------------
 */

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

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
        await SceneCtr.Instance.LoadScene(nm);

        switch (nm)
        {
            case "Main":
               GotoMainScene();
               break;
        }
    }

    private void GotoMainScene()
    {
        // 首次登陆
        var panel = _fsm.GetObj<LoginPanel>("loginPanel");
        if (panel != null)
        {
            _fsm.RemoveObj("loginPanel");
            Object.Destroy(panel.gameObject);
                    
            LuaCtr.Instance.InitLuaEnv();
        }
                
        ScreenCtr.Instance.SetMainCamera(GameConfig.MapCamera);
        PanelCtr.Instance.PanelOn("MainPanel", UILayer.BOTTOM_2, null);
    }
}