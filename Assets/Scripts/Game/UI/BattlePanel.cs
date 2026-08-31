/*--------------------------------------------------------------
 * File: BattlePanel.cs
 * Author: Wsw
 * Time: 2026/05/12 14:28:45
 *--------------------------------------------------------------
 */

/// <summary>
/// 战斗 HUD 面板（旧 Survival 的"点哪走哪"输入已随废弃玩法删除）。
/// 本面板持有虚拟摇杆（预制体挂 GameJoystick），OnOpen 时登记到 GameMgr.Battle，
/// 战斗侧（VampireView）经"查缓存 + 就绪事件"获取，双方互不持有引用（解耦）。
/// 塔防输入走 TTTT 测试驱动器；VS 玩法 HUD 将在此面板上扩展，见任务清单 2-10。
/// </summary>
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
