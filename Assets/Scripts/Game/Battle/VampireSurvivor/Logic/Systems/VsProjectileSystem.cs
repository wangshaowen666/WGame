/*--------------------------------------------------------------
 * File: VsProjectileSystem.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

/// <summary>
/// 弹幕系统（纯 C# 确定性）：直线推进 + 圆形点命中判定（距敌圆心 ≤ 敌半径，MVP 暴力 O(n×m)，阶段 7-1 空间网格）。
/// 命中产出 VsHitEvent（表现层飘字/特效），致死结算击杀数并掉落经验宝石。
/// </summary>
public sealed class VsProjectileSystem
{
    private readonly VampireLogic _logic;

    public VsProjectileSystem(VampireLogic logic)
    {
        _logic = logic;
    }

    /// <summary>发射弹幕（LogicWeapon 开火调用）：池取实体，从持有者当前位置沿单位方向射出</summary>
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

    /// <summary>弹幕飞行与命中（Tick 步骤 7）：命中扣血+击退冲量；致死计击杀并掉落；每帧每弹最多命中一个敌人</summary>
    public void Tick()
    {
        var projectiles = _logic.Projectiles;
        var enemies = _logic.Enemies;
        for (int i = 0; i < projectiles.Count; i++)
        {
            var b = projectiles[i];
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

                en.Hp -= b.Damage;
                en.KnockbackX += b.DirX * b.Knockback; // 击退冲量：沿弹幕飞行方向，多弹叠加
                en.KnockbackY += b.DirY * b.Knockback;
                if (en.Hp <= 0)
                {
                    _logic.AddKill();
                    _logic.SysDrop.Spawn(en); // 死亡掉落（经验宝石，逻辑实体；磁吸/拾取 2-7）
                }
                en.LastHitFrame = _logic.LastTickFrame; // 受击感知（表现层据此触发闪白）
                _logic.HitEvents.Add(new VsHitEvent(en.X, en.Y, b.Damage)); // 命中事件（表现层：飘字+特效）
                b.LastHitEnemyId = en.Id;
                b.Pierce--;
                break; // 每帧每弹最多命中一个敌人
            }
        }
    }

    /// <summary>弹幕状态进哈希（字段与顺序与拆分前一致：不含 OwnerId）</summary>
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
        }
    }
}
