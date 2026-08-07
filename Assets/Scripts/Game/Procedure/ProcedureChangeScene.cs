/*--------------------------------------------------------------
 * File: ProcedureChangeScene.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/22 18:26:05 
 *--------------------------------------------------------------
 */

using cfg;

public class ProcedureChangeScene : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        
        var nm = _fsm.GetData<string>(ProcedureKey.SceneName);
        if (string.IsNullOrEmpty(nm))
        {
            Log.Error("不存在的场景：", nm);
            return;
        }
        
        GameMgr.UI.PanelOn(DPnlId.LoadingPanel);
        GameMgr.OnSceneExit(GameConfig.MainScene.Equals(nm) ? SceneType.Main : SceneType.Battle);
        CoreMgr.Res.LoadSceneAsync(nm, null, OnSceneComplete);
    }
    
    private void OnSceneComplete()
    {
        var nm = _fsm.GetData<string>(ProcedureKey.SceneName);
        GameMgr.UI.PanelOff(DPnlId.LoadingPanel);
        switch (nm)
        {
            case GameConfig.MainScene:
                ChangeTo<ProcedureMain>();
                break;
            
            case GameConfig.BattleScene:
            case GameConfig.NetworkScene:
                ChangeTo<ProcedureBattle>();
                break;
        }
    }
}