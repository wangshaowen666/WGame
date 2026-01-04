/*--------------------------------------------------------------
 * File: ProcedureLaunch.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 16:25:31 
 *--------------------------------------------------------------
 */

using UnityEngine;
using UnityEngine.UI;

public class ProcedureLaunch : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        
        //ScreenCtr.Instance.Init();
        
        var panel = LoadLoginPanel();
        panel.SetTip("游戏启动中...", 0.1f);
        _fsm.SetObj("loginPanel", panel);

        if (AOTConfig.Standalone)
        {
            Procedure.Instance.RunProcedure<ProcedurePreload>();
        }
        else
        {
            Procedure.Instance.RunProcedure<ProcedureVersionCheck>();
        }
    }
    
    private LoginPanel LoadLoginPanel()
    {
        GameObject loginPrefab = Resources.Load<GameObject>(AOTConfig.LoginPath);
        if (loginPrefab != null)
        {
            GameObject loginInstance = Object.Instantiate(loginPrefab);
            loginInstance.name = "LoginRoot";
            
            var scaler = loginInstance.GetComponentInChildren<CanvasScaler>();
            var model = AOTConfig.ScreenSize;
            scaler.matchWidthOrHeight = Screen.width * 1.0f / Screen.height > model.x * 1.0f / model.y ? 1 : 0;
            
            return loginInstance.GetComponentInChildren<LoginPanel>();
        }
        else
        {
            throw new GameException("Failed to load LoginRoot prefab from Resources/LoginPanel/");
        }
    }
}