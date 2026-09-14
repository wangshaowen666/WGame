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
    private bool _weaponPanelOpen;  // 开局武器选择面板是否打开（暂停期间防重复 PanelOn）
    private bool _levelUpPanelOpen; // 选牌面板是否打开（暂停期间防重复 PanelOn）
    private bool _settleShown;      // 死亡结算是否已弹出（GameOver 后防重复 PanelOn）

    public VsHudPresenter(VampireLogic logic, LocalDriver driver)
    {
        _logic = logic;
        _driver = driver;
        _stageDurationSec = GameMgr.DataTable.TbVSStage.Get(VampireLogic.StageId).DurationSec;
        GameMgr.Event.Register<int>(GameEvent.VsLevelUpChosen, OnVsLevelUpChosen);
        GameMgr.Event.Register<int>(GameEvent.VsStartWeaponChosen, OnVsStartWeaponChosen);

        // 开局武器选择（3-1 扩展）：进战斗即暂停驱动，弹出武器选择面板（选完装备初始武器再恢复）
        if (_logic.PendingStartWeapon)
        {
            _driver.Pause();
            _weaponPanelOpen = true;
            GameMgr.UI.PanelOn(DPnlId.WeaponChoosePanel, BuildWeaponCards());
        }
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

    /// <summary>开局武器选择确认：装备所选武器（恢复驱动开始战斗），关面板</summary>
    private void OnVsStartWeaponChosen(int weaponId)
    {
        _logic.EquipStartWeapon(weaponId);
        _weaponPanelOpen = false;
        GameMgr.UI.PanelOff(DPnlId.WeaponChoosePanel);
        _driver.Resume();
        Log.Info("[吸血鬼] 开局武器:", weaponId);
    }

    /// <summary>组装开局武器卡片（武器表全量，Title=名字，Desc=效果说明）</summary>
    private List<VsWeaponCard> BuildWeaponCards()
    {
        var cards = new List<VsWeaponCard>();
        var weapons = GameMgr.DataTable.TbVSWeapon.DataList;
        for (int i = 0; i < weapons.Count; i++)
        {
            var cfg = weapons[i];
            cards.Add(new VsWeaponCard { WeaponId = cfg.Id, Title = cfg.Name, Desc = cfg.Desc });
        }
        return cards;
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

    /// <summary>组装当前选项的展示数据（表现层查表取名/描述 + 持有状态，下标对齐 CurrentChoices）。
    /// 武器项（3-2）：标题带等级（满级标记），描述展示本级→下级关键数值变化；被动项沿用属性说明</summary>
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
                if (owned == null)
                {
                    title = $"{cfg.Name}（新武器）";
                    desc = cfg.Desc;
                }
                else
                {
                    var nextLv = owned.Level + 1;
                    var next = GameMgr.DataTable.TbVSWeaponLevel.GetOrDefault(cfg.Id * 100 + nextLv);
                    var maxed = next == null; // 无下一级数据 = 已满级（选牌池理论已过滤，兜底防御）
                    title = maxed ? $"{cfg.Name}  Lv.{owned.Level}（满级）" : $"{cfg.Name}  Lv.{owned.Level}→Lv.{nextLv}";
                    desc = maxed ? cfg.Desc : DescribeWeaponLevelDiff(cfg.Id, owned.Level, next);
                }
            }
            else
            {
                var cfg = GameMgr.DataTable.TbVSPassive.Get(choices[i].ItemId);
                var owned = hero.Stats.FindPassive(cfg.Id);
                title = owned == null
                    ? $"{cfg.Name}（新被动）"
                    : $"{cfg.Name}  Lv.{owned.Level}→Lv.{owned.Level + 1}";
                desc = $"{AttrName(cfg.AttrType)} 每级 +{FormatAttr(cfg.AttrType, cfg.PerLevelValue)}（最高 {cfg.MaxLevel} 级）";
            }

            cards.Add(new VsLevelUpCard { Title = title, Desc = desc });
        }

        return cards;
    }

    /// <summary>武器本级→下级关键数值变化描述（3-2）：仅列有变化的项（伤害/冷却/弹数/穿透/速度/散射角/环绕/光环/重力）</summary>
    private static string DescribeWeaponLevelDiff(int weaponId, int curLevel, DVSWeaponLevel next)
    {
        var cur = GameMgr.DataTable.TbVSWeaponLevel.Get(weaponId * 100 + curLevel);
        var parts = new List<string>(8);

        if (next.Damage != cur.Damage)
            parts.Add($"伤害 {cur.Damage}→{next.Damage}");
        if (System.Math.Abs(next.CooldownSec - cur.CooldownSec) > 0.001f)
            parts.Add($"冷却 {cur.CooldownSec:0.##}→{next.CooldownSec:0.##}s");
        if (next.Amount != cur.Amount)
            parts.Add($"弹数 {cur.Amount}→{next.Amount}");
        if (next.Pierce != cur.Pierce)
            parts.Add($"穿透 {cur.Pierce}→{next.Pierce}");
        if (System.Math.Abs(next.ProjSpeed - cur.ProjSpeed) > 0.001f)
            parts.Add($"弹速 {cur.ProjSpeed:0.#}→{next.ProjSpeed:0.#}");
        if (System.Math.Abs(next.SpreadDeg - cur.SpreadDeg) > 0.001f)
            parts.Add($"扇形角 {cur.SpreadDeg:0.#}→{next.SpreadDeg:0.#}°");
        if (System.Math.Abs(next.OrbitRadius - cur.OrbitRadius) > 0.001f)
            parts.Add($"环绕半径 {cur.OrbitRadius:0.#}→{next.OrbitRadius:0.#}");
        if (System.Math.Abs(next.AuraRadius - cur.AuraRadius) > 0.001f)
            parts.Add($"光环半径 {cur.AuraRadius:0.#}→{next.AuraRadius:0.#}");

        return parts.Count > 0 ? string.Join("，", parts) : "综合强化";
    }

    /// <summary>属性类型 → 中文名（选牌卡片被动描述用，3-3）</summary>
    private static string AttrName(VSAttrType type)
    {
        switch (type)
        {
            case VSAttrType.MaxHp: return "生命上限";
            case VSAttrType.MoveSpeed: return "移速";
            case VSAttrType.Might: return "攻击力";
            case VSAttrType.Cooldown: return "冷却缩减";
            case VSAttrType.Area: return "范围";
            case VSAttrType.Amount: return "弹数";
            case VSAttrType.ProjSpeed: return "弹速";
            case VSAttrType.Duration: return "持续时间";
            case VSAttrType.Magnet: return "磁力";
            case VSAttrType.Armor: return "护甲";
            case VSAttrType.Regen: return "回复";
            case VSAttrType.Luck: return "幸运";
            case VSAttrType.Growth: return "成长";
            default: return type.ToString();
        }
    }

    /// <summary>属性每级加成格式化：百分比型属性 ×100 显示 %，数值型原样（3-3）</summary>
    private static string FormatAttr(VSAttrType type, float perLevel)
    {
        // 百分比语义（消费处 ×(1+x) 或 ×(1-x)）：Might/Cooldown/Area/ProjSpeed/Magnet/Growth/MoveSpeed
        if (type is VSAttrType.Might or VSAttrType.Cooldown or VSAttrType.Area
            or VSAttrType.ProjSpeed or VSAttrType.Magnet or VSAttrType.Growth or VSAttrType.MoveSpeed)
            return $"{perLevel * 100:0.#}%";
        return perLevel.ToString("0.#"); // 数值型：MaxHp/Amount/Regen/Armor
    }

    /// <summary>退出战斗：注销选牌事件 + 关闭武器选择/选牌/结算面板（幂等），清 HUD 引用</summary>
    public void Dispose()
    {
        GameMgr.Event.UnRegister<int>(GameEvent.VsLevelUpChosen, OnVsLevelUpChosen);
        GameMgr.Event.UnRegister<int>(GameEvent.VsStartWeaponChosen, OnVsStartWeaponChosen);
        if (_weaponPanelOpen) // 选武器中退出战斗：关面板（幂等），避免面板残留
        {
            _weaponPanelOpen = false;
            GameMgr.UI.PanelOff(DPnlId.WeaponChoosePanel);
        }
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
