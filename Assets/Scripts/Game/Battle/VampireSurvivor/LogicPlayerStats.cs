/*--------------------------------------------------------------
 * File: LogicPlayerStats.cs
 * Author: Wsw
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using cfg;

/// <summary>被动槽（逻辑层）：持有被动 id 与等级，升级 = Level+1</summary>
public class LogicPassive
{
    public int PassiveId;
    public int Level;
}

/// <summary>
/// 玩家属性系统（逻辑层）：基础值来自角色表，被动修改器叠加，统一结算函数。
/// 13 项属性以 VSAttrType 枚举值(1~13)为数组下标；内部统一 Fix 表示，消费处按语义取用。
/// 结算结果直接写回宿主 LogicHero 的 MaxHp/MoveSpeed（现有消费点零改动）；
/// 其余属性（Might/Cooldown/Amount 等）由武器/拾取等系统经 Get 取用。
/// </summary>
public class LogicPlayerStats
{
    private readonly LogicHero _hero;
    private readonly Fix[] _baseValues = new Fix[14];  // 基础值（角色表），下标 = (int)VSAttrType
    private readonly Fix[] _bonusValues = new Fix[14]; // 被动修改器叠加值
    private Fix _regenAccum;                           // 回复小数累积（Regen 为 HP/秒，满 1 点入账）

    /// <summary>已持有被动（有序 List，确定性遍历）</summary>
    public readonly List<LogicPassive> Passives = new();

    public LogicPlayerStats(LogicHero hero, DVSCharacter cfg)
    {
        _hero = hero;
        _baseValues[(int)VSAttrType.MaxHp] = Fix.FromInt((int)cfg.MaxHp);
        _baseValues[(int)VSAttrType.MoveSpeed] = Fix.FromDouble(cfg.MoveSpeed);
        Recalculate();
    }

    /// <summary>升级或新增被动并重算（升级选牌确认时调用；已有则升 1 级，否则新增）</summary>
    public void AddPassive(int passiveId)
    {
        for (int i = 0; i < Passives.Count; i++)
        {
            if (Passives[i].PassiveId == passiveId)
            {
                Passives[i].Level++;
                Recalculate();
                return;
            }
        }

        Passives.Add(new LogicPassive { PassiveId = passiveId, Level = 1 });
        Recalculate();
    }

    /// <summary>统一结算：基础值 + Σ(被动每级加成 × 等级)，加法叠加；写回宿主 MaxHp/MoveSpeed（MaxHp 提升只抬高上限并 clamp，不回血）</summary>
    public void Recalculate()
    {
        for (int i = 0; i < _bonusValues.Length; i++)
            _bonusValues[i] = Fix.Zero;

        for (int i = 0; i < Passives.Count; i++)
        {
            var slot = Passives[i];
            var cfg = GameMgr.DataTable.TbVSPassive.Get(slot.PassiveId);
            _bonusValues[(int)cfg.AttrType] += Fix.FromDouble(cfg.PerLevelValue) * Fix.FromInt(slot.Level);
        }

        _hero.MaxHp = Get(VSAttrType.MaxHp).Int;
        if (_hero.Hp > _hero.MaxHp)
            _hero.Hp = _hero.MaxHp;
        _hero.MoveSpeed = Get(VSAttrType.MoveSpeed);
    }

    /// <summary>每逻辑帧回复：Regen（HP/秒）逐帧累积，满 1 点入账并 clamp 上限（无被动时恒 0 早退）</summary>
    public void ApplyRegen(int logicFrameMs)
    {
        var regen = Get(VSAttrType.Regen);
        if (regen == Fix.Zero)
            return;

        _regenAccum += regen * Fix.FromInt(logicFrameMs) / Fix.FromInt(1000);
        if (_regenAccum < Fix.One)
            return;

        var gain = Fix.FromInt(_regenAccum.Int);
        _regenAccum -= gain;
        var hp = _hero.Hp + gain.Int;
        _hero.Hp = hp > _hero.MaxHp ? _hero.MaxHp : hp;
    }

    /// <summary>读结算后的属性值（Might/Cooldown 等百分比语义由消费处定义，如伤害×(1+Might)、冷却×(1-Cooldown)）</summary>
    public Fix Get(VSAttrType type)
    {
        return _baseValues[(int)type] + _bonusValues[(int)type];
    }
}
