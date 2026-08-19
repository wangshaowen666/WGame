/*--------------------------------------------------------------
 * File: BattleMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/26 14:53:41 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class BattleMgr : ManagerBase
{
    public BattleBase CurrentBattle => _battle;
    private BattleBase _battle;

    /// <summary>
    /// 进入帧同步塔防战斗（阶段 6 起由房间/匹配流程触发）
    /// </summary>
    public void EnterBattle()
    {
        _battle?.Dispose();
        _battle = new BattleTD();
        _battle.Init();
    }

    /// <summary>
    /// 退出战斗，清理表现层
    /// </summary>
    public void ExitBattle()
    {
        _battle?.Dispose();
        _battle = null;
    }

    public override void OnSceneExit(int sceneTp)
    {
        base.OnSceneExit(sceneTp);
        if (sceneTp == 2)
        {
            _battle?.Dispose();
            _battle = null;
        }
    }

    public override void OnGameRestart()
    {
        base.OnGameRestart();
        _battle?.Dispose();
        _battle = null;
    }
}
