/*--------------------------------------------------------------
 * File: VsLevelUpSystem.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using cfg;

/// <summary>
/// 经验与升级选牌系统（2-7/2-8，纯 C# 确定性）：
/// 拾取经验入账（×Growth 成长属性）并结算升级，每级产出一条升级事件（表现层 2-8 选牌面板 / 2-10 HUD 消费）；
/// 升级次数入队逐组三选一，表现层暂停驱动后经 ApplyChoice 消费。
/// </summary>
public sealed class VsLevelUpSystem
{
    /// <summary>每次升级可选的强化项数量</summary>
    public const int ChoiceCount = 3;

    private readonly VampireLogic _logic;
    private readonly int _expBase; // 升级经验曲线基数（1级->2级所需，关卡表）
    private readonly int _expStep; // 升级经验曲线步长（每级递增，关卡表）

    /// <summary>待选择组数（含当前展示组；连升多级逐组选择）</summary>
    public int PendingLevelUpCount { get; private set; }
    /// <summary>当前待选的强化项（三选一；空 = 无待选择）</summary>
    public readonly List<VsLevelUpChoice> CurrentChoices = new();
    /// <summary>是否有待选择的强化项（表现层据此暂停并弹出选牌面板）</summary>
    public bool HasPendingChoice => CurrentChoices.Count > 0;

    private readonly List<VsLevelUpChoice> _choicePool = new(); // 抽选缓冲（DrawChoices 复用，零分配）

    public VsLevelUpSystem(VampireLogic logic, int stageId)
    {
        _logic = logic;
        var stage = GameMgr.DataTable.TbVSStage.Get(stageId);
        _expBase = stage.ExpBase;
        _expStep = stage.ExpStep;
    }

    /// <summary>经验入账并结算升级：获取量 ×(1+Growth 成长属性)；可连升多级，每级产出一条升级事件（2-8 选牌消费）</summary>
    public void GainXp(LogicHero hero, long value)
    {
        var gain = (Fix.FromInt((int)value) * (Fix.One + hero.Stats.Get(VSAttrType.Growth))).Int;
        hero.Xp += gain;
        while (hero.Xp >= hero.XpToNext)
        {
            hero.Xp -= hero.XpToNext;
            hero.Level++;
            hero.XpToNext = CalcXpToNext(hero.Level);
            _logic.LevelUpEvents.Add(new VsLevelUpEvent(hero.Id, hero.Level));

            // 升级次数入队；无待展示组则立即抽选（已有组展示中则其应用后再抽，保证池状态最新）
            PendingLevelUpCount++;
            if (CurrentChoices.Count == 0)
                DrawChoices();
        }
    }

    /// <summary>升级经验曲线（关卡表线性）：n 级升至 n+1 所需 = ExpBase + ExpStep×(n-1)</summary>
    public long CalcXpToNext(int level)
    {
        return _expBase + _expStep * (level - 1);
    }

    /// <summary>
    /// 抽选强化项（2-8）：池 = 未拥有武器/被动 + 可升级项（已拥有未满级），部分 Fisher-Yates 无放回抽 ChoiceCount 项；
    /// 池不足时有多少抽多少，池为空则跳过本次选择（3-9 改为给金币）。
    /// </summary>
    private void DrawChoices()
    {
        CurrentChoices.Clear();
        BuildChoicePool();

        var draw = _choicePool.Count < ChoiceCount ? _choicePool.Count : ChoiceCount;
        for (int i = 0; i < draw; i++)
        {
            var j = i + _logic.Rng.NextInt(_choicePool.Count - i);
            (_choicePool[i], _choicePool[j]) = (_choicePool[j], _choicePool[i]);
            CurrentChoices.Add(_choicePool[i]);
        }

        if (draw == 0)
        {
            PendingLevelUpCount = 0;
            Log.Warning("[吸血鬼] 选牌池为空，跳过本次升级选择（3-9 改为给金币）");
        }
    }

    /// <summary>构建选牌池：武器/被动各遍历一遍（单人 MVP 取 Heroes[0]，联机选牌归属 5-3 拍板）</summary>
    private void BuildChoicePool()
    {
        _choicePool.Clear();
        var hero = _logic.Heroes[0];

        var weapons = GameMgr.DataTable.TbVSWeapon.DataList;
        for (int i = 0; i < weapons.Count; i++)
        {
            var cfg = weapons[i];
            var owned = hero.FindWeapon(cfg.Id);
            if (owned == null || GameMgr.DataTable.TbVSWeaponLevel.GetOrDefault(cfg.Id * 100 + owned.Level + 1) != null)
                _choicePool.Add(new VsLevelUpChoice(true, cfg.Id));
        }

        var passives = GameMgr.DataTable.TbVSPassive.DataList;
        for (int i = 0; i < passives.Count; i++)
        {
            var cfg = passives[i];
            var owned = hero.Stats.FindPassive(cfg.Id);
            if (owned == null || owned.Level < cfg.MaxLevel)
                _choicePool.Add(new VsLevelUpChoice(false, cfg.Id));
        }
    }

    /// <summary>
    /// 应用玩家选择（表现层选牌面板确认时调用）：武器已拥有则升级、否则新增；被动走 AddPassive（同语义）。
    /// 应用后仍有排队升级组则立即抽选下一组（HasPendingChoice 保持 true，面板继续展示）。
    /// </summary>
    public void ApplyChoice(int index)
    {
        if (CurrentChoices.Count == 0)
            return;
        if (index < 0 || index >= CurrentChoices.Count)
        {
            Log.Error("[吸血鬼] 非法的选牌下标:", index);
            return;
        }

        var hero = _logic.Heroes[0];
        var choice = CurrentChoices[index];
        if (choice.IsWeapon)
        {
            var owned = hero.FindWeapon(choice.ItemId);
            if (owned != null)
                owned.LevelUp();
            else
                hero.Weapons.Add(new LogicWeapon(choice.ItemId));
        }
        else
        {
            hero.Stats.AddPassive(choice.ItemId);
        }

        PendingLevelUpCount--;
        if (PendingLevelUpCount > 0)
            DrawChoices();
        else
            CurrentChoices.Clear();
    }

    /// <summary>选牌状态进哈希（应用选择影响后续演算；字段与顺序与拆分前一致）</summary>
    public void HashState(ref ulong h)
    {
        VsHash.Mix(ref h, PendingLevelUpCount);
        for (int i = 0; i < CurrentChoices.Count; i++)
        {
            VsHash.Mix(ref h, CurrentChoices[i].IsWeapon ? 1 : 0);
            VsHash.Mix(ref h, CurrentChoices[i].ItemId);
        }
    }
}
