/*--------------------------------------------------------------
 * File: BattlePanel.cs
 * Author: Wsw
 * Time: 2026/05/12 14:28:45
 *--------------------------------------------------------------
 */

public class BattlePanel : UIPanelBase
{
    public override void OnOpen(object userData = null)
    {
        base.OnOpen(userData);
        
        GameMgr.Battle.RegisterJoystick(GetComponentInChildren<GameJoystick>());
    }

    public override void OnRecycle()
    {
        GameMgr.Battle.ClearJoystick();
        base.OnRecycle();
    }
}
