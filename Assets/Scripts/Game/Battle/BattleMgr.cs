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
    public BattleView CurrentBattleView => _battleView;
    private BattleView _battleView;

    /// <summary>
    /// 预置的战斗模式（进入战斗场景前的临时入口设置；ProcedureBattle 读取后传入 EnterBattle）。
    /// 默认塔防，兼容旧的"仅联机测试"路径（TTTT 直接调 EnterBattle() 默认塔防）。
    /// </summary>
    public BattleMode PendingMode { get; set; } = BattleMode.VampireSurvivor;

    /// <summary>
    /// 当前战斗 UI 登记的战斗摇杆（BattlePanel.OnOpen 登记 / OnRecycle 清空）。
    /// 作为 UI 与战斗表现层之间的解耦中间层：VampireView 经"查缓存 + 就绪事件"获取，双方互不持有引用。
    /// </summary>
    public GameJoystick Joystick { get; private set; }

    /// <summary>战斗 UI 登记摇杆：缓存 + 广播就绪事件（订阅者可能尚未 Init，靠查询兜底）</summary>
    public void RegisterJoystick(GameJoystick joystick)
    {
        Joystick = joystick;
        GameMgr.Event.Send(GameEvent.VsJoystickReady, joystick);
    }

    /// <summary>战斗 UI 关闭时清空登记（防池化复用面板把摇杆残留给下一局）</summary>
    public void ClearJoystick()
    {
        Joystick = null;
    }

    /// <summary>
    /// 进入帧同步塔防战斗。前置条件：收到 StartGamePush（全员就绪，起始帧+种子已就位）
    /// </summary>
    public void EnterBattle()
    {
        EnterBattle(BattleMode.TowerDefense);
    }

    /// <summary>
    /// 按模式进入战斗：
    /// - TowerDefense：联机帧同步，前置校验 Room.StartFrame（需收到开战推送）
    /// - VampireSurvivor：单机玩法，无房间/联机依赖，跳过 Room.StartFrame 校验（本地驱动器驱动）
    /// </summary>
    public void EnterBattle(BattleMode mode)
    {
        switch (mode)
        {
            case BattleMode.TowerDefense:
                if (GameMgr.Room.StartFrame == 0)
                {
                    Log.Warning("未收到开战推送（需房间内全员就绪），拒绝进入战斗");
                    return;
                }
                break;

            case BattleMode.VampireSurvivor:
                // 单机玩法：无房间/联机依赖，直接进入（LocalDriver 本地固定步长驱动，阶段 1-3）
                break;

            default:
                Log.Error("未知战斗模式:", mode);
                return;
        }

        _battleView?.Dispose();
        switch (mode)
        {
            case BattleMode.TowerDefense:
                _battleView = new TdView();
                break;
            case BattleMode.VampireSurvivor:
                _battleView = new VampireView();
                break;
        }
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
