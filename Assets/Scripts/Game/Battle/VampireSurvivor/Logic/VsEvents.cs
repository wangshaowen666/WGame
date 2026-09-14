/*--------------------------------------------------------------
 * File: VsEvents.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

/// <summary>单帧玩家输入（逻辑层输入单元）：IBattleInput 产出，帧边界采样进 Tick</summary>
public struct VsInput
{
    public int PlayerIndex;
    public Fix DirX;
    public Fix DirY;
}

/// <summary>弹幕命中事件（逻辑层产出、表现层消费）：每次弹幕命中一条，供伤害飘字/命中特效锚定</summary>
public readonly struct VsHitEvent
{
    public readonly Fix X;
    public readonly Fix Y;
    public readonly long Damage;

    public VsHitEvent(Fix x, Fix y, long damage)
    {
        X = x;
        Y = y;
        Damage = damage;
    }
}

/// <summary>升级事件（逻辑层产出、表现层消费）：拾取经验触发升级时产生（一帧可多条=连升），2-8 选牌面板 / 2-10 HUD 消费</summary>
public readonly struct VsLevelUpEvent
{
    public readonly int HeroId;
    public readonly int NewLevel;

    public VsLevelUpEvent(int heroId, int newLevel)
    {
        HeroId = heroId;
        NewLevel = newLevel;
    }
}

/// <summary>选牌强化项（逻辑层，2-8）：IsWeapon 区分武器/被动，ItemId 为对应表 id</summary>
public readonly struct VsLevelUpChoice
{
    public readonly bool IsWeapon;
    public readonly int ItemId;

    public VsLevelUpChoice(bool isWeapon, int itemId)
    {
        IsWeapon = isWeapon;
        ItemId = itemId;
    }
}

/// <summary>
/// 宝箱开启事件（逻辑层产出、表现层消费，3-6）：拾取宝箱即结算随机升级/保底治疗，
/// 事件供表现层飘奖励公告（文案由表现层查表组装，逻辑层不产字符串）。
/// </summary>
public readonly struct VsChestEvent
{
    public readonly int HeroId;
    public readonly bool IsWeapon; // true=武器 false=被动（保底治疗时无意义）
    public readonly int ItemId;    // 武器/被动表 id（保底治疗时无意义）
    public readonly int OldLevel;  // 升级前等级（新增项=0）
    public readonly int NewLevel;  // 升级后等级（新增项=1）
    public readonly long Heal;     // 保底治疗量（>0 = 本次为保底治疗）

    public VsChestEvent(int heroId, bool isWeapon, int itemId, int oldLevel, int newLevel, long heal)
    {
        HeroId = heroId;
        IsWeapon = isWeapon;
        ItemId = itemId;
        OldLevel = oldLevel;
        NewLevel = newLevel;
        Heal = heal;
    }
}

/// <summary>状态哈希混合工具（FNV-1a 64）：VampireLogic 与各系统的 HashState 共用；混合顺序即确定性，调用方不得重排</summary>
public static class VsHash
{
    public static void Mix(ref ulong h, long v)
    {
        h = (h ^ (ulong)v) * 1099511628211UL;
    }
}
