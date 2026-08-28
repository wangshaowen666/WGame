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
    public BattleViewBase CurrentBattleView => _battleView;
    private BattleViewBase _battleView;

    /// <summary>
    /// 进入帧同步塔防战斗。前置条件：收到 StartGamePush（全员就绪，起始帧+种子已就位）
    /// </summary>
    public void EnterBattle()
    {
        if (GameMgr.Room.StartFrame == 0)
        {
            Log.Warning("未收到开战推送（需房间内全员就绪），拒绝进入战斗");
            return;
        }
        _battleView?.Dispose();
        _battleView = new TdView();
        _battleView.Init();
    }

    /// <summary>
    /// 退出战斗，清理表现层
    /// </summary>
    public void ExitBattle()
    {
        _battleView?.Dispose();
        _battleView = null;
    }

    public override void OnSceneExit(string sceneNm)
    {
        // 离开战斗类场景（战斗/联机战斗）时清理战斗
        if (sceneNm == GameConfig.BattleScene)
        {
            _battleView?.Dispose();
            _battleView = null;
        }
    }

    public override void OnGameRestart()
    {
        base.OnGameRestart();
        _battleView?.Dispose();
        _battleView = null;
    }
}
