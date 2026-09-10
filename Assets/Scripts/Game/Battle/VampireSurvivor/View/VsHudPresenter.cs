/*--------------------------------------------------------------
 * File: VsHudPresenter.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using cfg;

/// <summary>
/// 吸血鬼幸存者 HUD 与战斗面板流程（表现层）：
/// - 战斗 HUD 刷新（BattlePanel 上 VS 部分，2-10；面板异步加载，逐帧拉取直至就绪）
/// - 升级选牌面板（2-8）：有待选项暂停驱动并弹出，确认经 VsLevelUpChosen 事件回推，
///   连升多组换题（VsLevelUpChoicesChanged），选完关面板恢复战斗
/// - 死亡结算面板（2-11）：GameOver 后暂停驱动并弹出，统计快照经 userData 传入
/// </summary>
public sealed class VsHudPresenter
{
    private readonly VampireLogic _logic;
    private readonly LocalDriver _driver;
    private readonly float _stageDurationSec; // 关卡存活时长上限（0=无限，HUD 据此显示倒计时/正计时）

    private BattlePanel _hud;
    private bool _levelUpPanelOpen; // 选牌面板是否打开（暂停期间防重复 PanelOn）
    private bool _settleShown;      // 死亡结算是否已弹出（GameOver 后防重复 PanelOn）

    public VsHudPresenter(VampireLogic logic, LocalDriver driver)
    {
        _logic = logic;
        _driver = driver;
        _stageDurationSec = GameMgr.DataTable.TbVSStage.Get(VampireLogic.StageId).DurationSec;
        GameMgr.Event.Register<int>(GameEvent.VsLevelUpChosen, OnVsLevelUpChosen);
    }

    /// <summary>逻辑帧驱动：HUD 刷新 + 选牌/结算面板流程（由 VampireView.OnFrame 调用）</summary>
    public void OnFrame()
    {
        RefreshHud();

        // 升级选牌（2-8）：有待选项则暂停驱动并弹出选牌面板（面板异步加载，选项数据经 userData 传入）
        if (_logic.SysLevelUp.HasPendingChoice && !_levelUpPanelOpen)
        {
            _driver.Pause();
            GameMgr.UI.PanelOn(DPnlId.LevelUpPanel, BuildLevelUpCards());
            _levelUpPanelOpen = true;
        }

        // 死亡结算（2-11）：逻辑层判定 GameOver 后暂停驱动并弹出结算面板（统计快照经 userData 传入；临时面板阶段 6 正式化）
        if (_logic.GameOver && !_settleShown)
        {
            _settleShown = true;
            _driver.Pause();
            GameMgr.UI.PanelOn(DPnlId.SettlePanel, new VsSettleData
            {
                SurviveSec = _logic.LastTickFrame * VampireLogic.LogicFrameMs / 1000,
                KillCount = _logic.KillCount,
                Level = _logic.Heroes[0].Level,
            });
        }
    }

    /// <summary>选牌面板确认：应用选择；仍有排队升级组则推送新选项换题（面板保持打开），否则关面板恢复战斗</summary>
    private void OnVsLevelUpChosen(int index)
    {
        _logic.SysLevelUp.ApplyChoice(index);
        if (_logic.SysLevelUp.HasPendingChoice)
        {
            GameMgr.Event.Send(GameEvent.VsLevelUpChoicesChanged, BuildLevelUpCards());
            return;
        }

        _levelUpPanelOpen = false;
        GameMgr.UI.PanelOff(DPnlId.LevelUpPanel);
        _driver.Resume();
    }

    /// <summary>刷新战斗 HUD（2-10，逻辑帧驱动；选牌暂停期间驱动停步进，HUD 自然冻结）。倒计时 = 关卡表 DurationSec（0=无限则显示存活正计时）</summary>
    private void RefreshHud()
    {
        if (_hud == null)
            _hud = GameMgr.UI.HasPanel(DPnlId.BattlePanel) as BattlePanel;
        if (_hud == null)
            return;

        var hero = _logic.Heroes[0]; // 单机单人；联机 HUD 归属 5-3 拍板
        var elapsed = _logic.LastTickFrame * (VampireLogic.LogicFrameMs / 1000f);
        var countdown = _stageDurationSec > 0f;
        var timeSec = countdown ? System.Math.Max(0f, _stageDurationSec - elapsed) : elapsed;
        _hud.UpdateHud(hero.Hp, hero.MaxHp, hero.Level, hero.Xp, hero.XpToNext, _logic.KillCount, timeSec, countdown);
    }

    /// <summary>组装当前选项的展示数据（表现层查表取名/描述 + 持有状态，下标对齐 CurrentChoices）</summary>
    private List<VsLevelUpCard> BuildLevelUpCards()
    {
        var cards = new List<VsLevelUpCard>();
        var hero = _logic.Heroes[0]; // 单机单人；联机选牌归属 5-3 拍板
        var choices = _logic.SysLevelUp.CurrentChoices;
        for (int i = 0; i < choices.Count; i++)
        {
            string title;
            string desc;
            if (choices[i].IsWeapon)
            {
                var cfg = GameMgr.DataTable.TbVSWeapon.Get(choices[i].ItemId);
                var owned = hero.FindWeapon(choices[i].ItemId);
                title = owned == null
                    ? $"{cfg.Name}（新武器）"
                    : $"{cfg.Name}  Lv.{owned.Level}→Lv.{owned.Level + 1}";
                desc = cfg.Desc;
            }
            else
            {
                var cfg = GameMgr.DataTable.TbVSPassive.Get(choices[i].ItemId);
                var owned = hero.Stats.FindPassive(choices[i].ItemId);
                title = owned == null
                    ? $"{cfg.Name}（新被动）"
                    : $"{cfg.Name}  Lv.{owned.Level}→Lv.{owned.Level + 1}";
                desc = $"[{cfg.AttrType}] 每级 +{cfg.PerLevelValue}（最高 {cfg.MaxLevel} 级）";
            }

            cards.Add(new VsLevelUpCard { Title = title, Desc = desc });
        }

        return cards;
    }

    /// <summary>退出战斗：注销选牌事件 + 关闭选牌/结算面板（幂等），清 HUD 引用</summary>
    public void Dispose()
    {
        GameMgr.Event.UnRegister<int>(GameEvent.VsLevelUpChosen, OnVsLevelUpChosen);
        if (_levelUpPanelOpen) // 选牌中退出战斗：关面板（幂等），避免面板残留
        {
            _levelUpPanelOpen = false;
            GameMgr.UI.PanelOff(DPnlId.LevelUpPanel);
        }
        if (_settleShown) // 结算中退出战斗：关结算面板（幂等）
        {
            _settleShown = false;
            GameMgr.UI.PanelOff(DPnlId.SettlePanel);
        }
        _hud = null;
    }
}
