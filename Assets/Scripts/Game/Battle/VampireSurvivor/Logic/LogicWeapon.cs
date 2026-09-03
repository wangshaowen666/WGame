/*--------------------------------------------------------------
 * File: LogicWeapon.cs
 * Author: Wsw
 *--------------------------------------------------------------
 */

using System;
using cfg;

/// <summary>
/// 武器实例（逻辑层）：等级/冷却计时/开火逻辑。范式(behavior)×目标策略(targetStrategy) 由表驱动，
/// 数值 = 武器等级表（TbVSWeaponLevel，id=武器id*100+等级）× 属性系统加成（2-2）。
/// 开火由 Logic 在 Tick 内固定序遍历（英雄序 → 武器序）驱动。
/// </summary>
public class LogicWeapon
{
    public readonly int WeaponId;
    public readonly DVSWeapon Cfg; // 模板（范式/目标策略，只读）

    private DVSWeaponLevel _levelCfg; // 当前等级数值（升级时刷新）
    private int _level = 1;
    private int _cooldownFrames;      // 距下次开火剩余帧

    public LogicWeapon(int weaponId)
    {
        WeaponId = weaponId;
        Cfg = GameMgr.DataTable.TbVSWeapon.Get(weaponId);
        _levelCfg = GameMgr.DataTable.TbVSWeaponLevel.Get(weaponId * 100 + 1);
    }

    public int Level => _level;

    /// <summary>升级 1 级（升级选牌确认时调用）；无下一级数据即已满级，保持不变</summary>
    public void LevelUp()
    {
        var next = GameMgr.DataTable.TbVSWeaponLevel.GetOrDefault(WeaponId * 100 + _level + 1);
        if (next == null)
            return;

        _level++;
        _levelCfg = next;
    }

    /// <summary>每逻辑帧推进：冷却计时，就绪且能获取目标则开火并重置冷却</summary>
    public void Tick(VampireLogic logic, LogicHero owner)
    {
        if (_cooldownFrames > 0)
        {
            _cooldownFrames--;
            return;
        }

        // 目标策略分发（当前实现 Nearest；Random/Facing/MoveDir 阶段 3-1 随武器范式补全）
        var target = Cfg.TargetStrategy == VSTargetStrategy.Nearest ? FindNearest(logic, owner) : null;
        if (target == null)
            return;

        switch (Cfg.Behavior)
        {
            case VSWeaponBehavior.Projectile:
                FireProjectile(logic, owner, target);
                break;
        }

        _cooldownFrames = CalcCooldownFrames(owner);
    }

    /// <summary>最近存活敌人（距离平方比较，无敌人返回 null：冷却保持就绪，出现目标即刻开火）</summary>
    private LogicEnemy FindNearest(VampireLogic logic, LogicHero owner)
    {
        LogicEnemy best = null;
        var bestSq = Fix.Zero;
        var enemies = logic.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            var en = enemies[i];
            if (en.Hp <= 0)
                continue;

            var dx = en.X - owner.X;
            var dy = en.Y - owner.Y;
            var dSq = dx * dx + dy * dy;
            if (best == null || dSq < bestSq)
            {
                best = en;
                bestSq = dSq;
            }
        }

        return best;
    }

    /// <summary>直线弹幕：伤害×(1+Might)、弹数+Amount、弹速换算每帧位移；多弹同向连发（MVP 重叠结算，扇形/间隔阶段 3-1）</summary>
    private void FireProjectile(VampireLogic logic, LogicHero owner, LogicEnemy target)
    {
        var stats = owner.Stats;
        var dirX = target.X - owner.X;
        var dirY = target.Y - owner.Y;
        var len = Fix.Sqrt(dirX * dirX + dirY * dirY);
        if (len == Fix.Zero)
        {
            dirX = Fix.One;
            dirY = Fix.Zero;
            len = Fix.One;
        }

        var damage = (Fix.FromInt(_levelCfg.Damage) * (Fix.One + stats.Get(VSAttrType.Might))).Int;
        var speed = Fix.FromDouble(_levelCfg.ProjSpeed) * Fix.FromInt(VampireLogic.LogicFrameMs) / Fix.FromInt(1000); // 单位/秒 → 单位/帧
        var life = (Fix.FromDouble(_levelCfg.DurationSec) * Fix.FromInt(1000) / Fix.FromInt(VampireLogic.LogicFrameMs)).Int;
        var amount = _levelCfg.Amount + stats.Get(VSAttrType.Amount).Int;

        for (int i = 0; i < amount; i++)
            logic.SpawnProjectile(owner, dirX / len, dirY / len, speed, damage, _levelCfg.Pierce, life, Fix.FromDouble(Cfg.Knockback));
    }

    /// <summary>冷却帧数：CooldownSec×(1-Cooldown 属性)，秒→帧向下取整，最少 1 帧防零冷却连发</summary>
    private int CalcCooldownFrames(LogicHero owner)
    {
        var cd = Fix.FromDouble(_levelCfg.CooldownSec) * (Fix.One - owner.Stats.Get(VSAttrType.Cooldown));
        var frames = (cd * Fix.FromInt(1000) / Fix.FromInt(VampireLogic.LogicFrameMs)).Int;
        return frames < 1 ? 1 : frames;
    }
}
