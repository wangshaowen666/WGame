/*--------------------------------------------------------------
 * File: VsProjectileSystem.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

using System.Collections.Generic;

/// <summary>
/// 弹幕系统（纯 C# 确定性）：直线推进 + 圆形点命中判定（距敌圆心 ≤ 敌半径，MVP 暴力 O(n×m)，阶段 7-1 空间网格）。
/// 弹道变体（3-1）：Gravity=0 直线；Gravity>0 抛物线（斧头）——竖直高度独立积分（vy -= g，g=表配单位/帧²），
/// 空中不判定命中，落地（Height≤0）时对落点圆形范围一次判定（穿透个数为上限，逐个命中），判定完回收。
/// 命中产出 VsHitEvent（表现层飘字/特效），致死结算击杀数并掉落经验宝石。
/// </summary>
public sealed class VsProjectileSystem
{
    private readonly VampireLogic _logic;

    public VsProjectileSystem(VampireLogic logic)
    {
        _logic = logic;
    }

    /// <summary>发射直线弹幕（LogicWeapon 开火调用）：池取实体，从持有者当前位置沿单位方向射出</summary>
    public void Spawn(LogicHero owner, Fix dirX, Fix dirY, Fix speed, long damage, int pierce, int lifeFrames, Fix knockback)
    {
        var b = CoreMgr.ClassPool.Get<LogicProjectile>();
        b.Id = _logic.AllocId();
        b.X = owner.X;
        b.Y = owner.Y;
        b.DirX = dirX;
        b.DirY = dirY;
        b.MoveSpeed = speed;
        b.Damage = damage;
        b.Pierce = pierce;
        b.LifeFrames = lifeFrames;
        b.OwnerId = owner.Id;
        b.Knockback = knockback;
        _logic.Projectiles.Add(b);
    }

    /// <summary>
    /// 发射抛物线弹幕（3-1 斧头）：朝 horiz 方向以水平速度推进 + 竖直上抛（初速 vertVel，重力 gravity），
    /// 空中不命中；落地对落点圆形范围判定（半径 = 落点判定半径，可命中 landHits 个），判定完回收。
    /// </summary>
    public void SpawnArc(LogicHero owner, Fix horizX, Fix horizY, Fix horizSpeed, Fix vertVel, Fix gravity,
        long damage, int landHits, int lifeFrames, Fix knockback, Fix landRadius)
    {
        var b = CoreMgr.ClassPool.Get<LogicProjectile>();
        b.Id = _logic.AllocId();
        b.X = owner.X;
        b.Y = owner.Y;
        b.GroundX = owner.X;
        b.GroundY = owner.Y;
        b.HorizX = horizX;
        b.HorizY = horizY;
        b.MoveSpeed = horizSpeed;
        b.VertVel = vertVel;
        b.Gravity = gravity;
        b.Height = Fix.Zero;
        b.Damage = damage;
        b.Pierce = landHits;
        b.LifeFrames = lifeFrames;
        b.OwnerId = owner.Id;
        b.Knockback = knockback;
        // 表现层渲染位置：X=水平落点投影，Y=竖直高度（EntityViewBase 的 XY 平面内）；
        // DirX/DirY 记录水平朝向（表现层朝向用）
        b.DirX = horizX;
        b.DirY = horizY;
        _landRadius = landRadius; // 本帧落地判定半径（同武器一次开火一致；存系统级临时值，见 TickArc 说明）
        _logic.Projectiles.Add(b);
    }

    private Fix _landRadius; // 抛物线落地判定半径（SpawnArc 传入，落地帧消费）

    /// <summary>弹幕飞行与命中（Tick 步骤 7）：命中扣血+击退冲量；致死计击杀并掉落；每帧每弹最多命中一个敌人</summary>
    public void Tick()
    {
        var projectiles = _logic.Projectiles;
        var enemies = _logic.Enemies;
        for (int i = 0; i < projectiles.Count; i++)
        {
            var b = projectiles[i];
            if (b.Gravity > Fix.Zero)
            {
                TickArc(b, enemies);
                continue;
            }

            b.X += b.DirX * b.MoveSpeed;
            b.Y += b.DirY * b.MoveSpeed;
            b.LifeFrames--;

            for (int e = 0; e < enemies.Count; e++)
            {
                var en = enemies[e];
                if (en.Hp <= 0 || en.Id == b.LastHitEnemyId)
                    continue;

                var dx = en.X - b.X;
                var dy = en.Y - b.Y;
                var r = en.Radius;
                if (dx * dx + dy * dy > r * r)
                    continue;

                ApplyHit(en, b.DirX, b.DirY, b.Damage, b.Knockback);
                b.LastHitEnemyId = en.Id;
                b.Pierce--;
                break; // 每帧每弹最多命中一个敌人
            }
        }
    }

    /// <summary>抛物线推进（3-1）：水平匀速 + 竖直积分（vy -= gravity），落地对落点范围判定（最多 Pierce 个）后回收</summary>
    private void TickArc(LogicProjectile b, List<LogicEnemy> enemies)
    {
        b.LifeFrames--;

        // 水平推进（落点基准）+ 竖直积分
        b.GroundX += b.HorizX * b.MoveSpeed;
        b.GroundY += b.HorizY * b.MoveSpeed;
        b.VertVel -= b.Gravity;
        b.Height += b.VertVel;
        // 表现位置：X/GroundX=水平，Y=高度（表现层读 Height 画抛物线；逻辑命中只看落点）
        b.X = b.GroundX;
        b.Y = b.GroundY;

        if (b.Height > Fix.Zero && b.LifeFrames > 0)
            return; // 仍在空中

        // 落地（或超时）：对落点圆形范围判定，按敌序逐个命中（上限 Pierce），一次结算完即回收
        var hits = b.Pierce;
        for (int e = 0; e < enemies.Count && hits > 0; e++)
        {
            var en = enemies[e];
            if (en.Hp <= 0)
                continue;

            var dx = en.X - b.GroundX;
            var dy = en.Y - b.GroundY;
            var r = en.Radius + _landRadius;
            if (dx * dx + dy * dy > r * r)
                continue;

            ApplyHit(en, b.HorizX, b.HorizY, b.Damage, b.Knockback);
            hits--;
        }
        b.Pierce = 0; // 判定完成，交由帧末集中清扫回收（LifeFrames 与 Pierce 条件同直线弹一致）
    }

    /// <summary>直接命中结算（瞬时判定型武器共用，3-1 鞭子横扫）：伤害+击退+受击感知+事件；致死计击杀并掉落</summary>
    public void HitDirect(LogicEnemy en, Fix dirX, Fix dirY, long damage, Fix knockback)
    {
        ApplyHit(en, dirX, dirY, damage, knockback);
    }

    /// <summary>命中结算（直线/落地/横扫共用）：扣血+击退冲量+受击感知+命中事件；致死计击杀并掉落</summary>
    private void ApplyHit(LogicEnemy en, Fix dirX, Fix dirY, long damage, Fix knockback)
    {
        en.Hp -= damage;
        en.KnockbackX += dirX * knockback; // 击退冲量：沿弹幕方向，多弹叠加
        en.KnockbackY += dirY * knockback;
        if (en.Hp <= 0)
        {
            _logic.AddKill();
            _logic.SysDrop.Spawn(en); // 死亡掉落（经验宝石，逻辑实体；磁吸/拾取 2-7）
        }
        en.LastHitFrame = _logic.LastTickFrame; // 受击感知（表现层据此触发闪白）
        _logic.HitEvents.Add(new VsHitEvent(en.X, en.Y, damage)); // 命中事件（表现层：飘字+特效）
    }

    /// <summary>弹幕状态进哈希（含抛物线运行时字段：影响位置演算；不含 OwnerId）</summary>
    public void HashState(ref ulong h)
    {
        var projectiles = _logic.Projectiles;
        for (int i = 0; i < projectiles.Count; i++)
        {
            var b = projectiles[i];
            VsHash.Mix(ref h, b.Id);
            VsHash.Mix(ref h, b.X.Raw);
            VsHash.Mix(ref h, b.Y.Raw);
            VsHash.Mix(ref h, b.DirX.Raw);
            VsHash.Mix(ref h, b.DirY.Raw);
            VsHash.Mix(ref h, b.MoveSpeed.Raw);
            VsHash.Mix(ref h, b.Damage);
            VsHash.Mix(ref h, b.Pierce);
            VsHash.Mix(ref h, b.LifeFrames);
            VsHash.Mix(ref h, b.LastHitEnemyId);
            VsHash.Mix(ref h, b.Knockback.Raw);
            VsHash.Mix(ref h, b.Gravity.Raw);
            VsHash.Mix(ref h, b.VertVel.Raw);
            VsHash.Mix(ref h, b.HorizX.Raw);
            VsHash.Mix(ref h, b.HorizY.Raw);
            VsHash.Mix(ref h, b.GroundX.Raw);
            VsHash.Mix(ref h, b.GroundY.Raw);
            VsHash.Mix(ref h, b.Height.Raw);
        }
    }
}
