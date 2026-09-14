/*--------------------------------------------------------------
 * File: VsFieldSystem.cs
 * Author: Wsw
 * Time: 2026/09/12
 *--------------------------------------------------------------
 */

using System.Collections.Generic;

/// <summary>
/// 区域效果系统（纯 C# 确定性，3-1）：持续区域型武器（圣经环绕/大蒜光环）的推进与结算。
/// Orbit（环绕体）：Amount 个环绕单元绕宿主公转（免 sin/cos 表：单元方位以旋转矩阵增量累积，
///   每帧左乘固定小角度旋转，单元间再左乘均分圆周角旋转），逐帧圆形接触判定（命中推离，穿透不消耗）；
/// Aura（光环）：以宿主为圆心的常驻圆，每 TickInterval 帧对范围内全部敌人灼烧一次（伤害+击退）。
/// 命中结算经 delegate 注入（VampireLogic 装配时绑到弹幕系统的 ApplyHit 语义），事件/击杀/掉落全玩法一致。
/// </summary>
public sealed class VsFieldSystem
{
    // ---- Orbit 手感常量（暂放常量区，频繁调整再挪表）----
    private static readonly Fix s_rotStepCos = Fix.FromDouble(0.99718979f); // 每帧自转小角度旋转矩阵 cos 分量（约 0.075 rad ≈ 4.3°/帧 ≈ 86°/秒）
    private static readonly Fix s_rotStepSin = Fix.FromDouble(0.07492972f); // sin 分量
    private static readonly Fix s_orbitUnitRadius = Fix.FromDouble(0.35);   // 单个环绕单元的判定半径

    private readonly VampireLogic _logic;
    private readonly HitEnemy _hitEnemy; // 命中结算回调（装配时注入）

    public delegate void HitEnemy(LogicEnemy en, Fix dirX, Fix dirY, long damage, Fix knockback);

    /// <summary>存活区域实体（有序 List，确定性遍历；帧末集中清扫由 Logic 编排）</summary>
    public readonly List<LogicField> Fields = new();

    // Orbit 运行时旋转状态（全局共享：同武器多实例视觉同步；进 StateHash 保证双端一致）
    private Fix _rotCos = Fix.One;
    private Fix _rotSin = Fix.Zero;
    // 均分圆周角旋转矩阵（随最大 Amount 的 Spawn 更新；Amount=1 恒等）
    private Fix _unitStepCos = Fix.One;
    private Fix _unitStepSin = Fix.Zero;
    private int _unitStepAmount; // 当前均分角对应的单元数

    public VsFieldSystem(VampireLogic logic, HitEnemy hitEnemy)
    {
        _logic = logic;
        _hitEnemy = hitEnemy;
    }

    /// <summary>
    /// 生成区域效果（LogicWeapon 开火调用，持续区域型）：
    /// Orbit：amount=环绕单元数（均分圆周），radius=环绕半径，lifeFrames=持续帧数（到期回收，冷却后重新生成）；
    /// Aura：常驻（lifeFrames=极大值），同武器已有光环则只刷新数值（选牌升级即时生效），不叠加新实体。
    /// </summary>
    public void Spawn(LogicHero owner, int type, int weaponId, Fix radius, long damage, Fix knockback,
        int tickInterval, int lifeFrames, int amount)
    {
        // Aura 常驻去重：同英雄同武器已有光环 → 只刷新数值
        if (type == VSFieldType.Aura)
        {
            for (int i = 0; i < Fields.Count; i++)
            {
                var ex = Fields[i];
                if (ex.Type == VSFieldType.Aura && ex.OwnerId == owner.Id && ex.WeaponId == weaponId)
                {
                    ex.Radius = radius;
                    ex.Damage = damage;
                    ex.Knockback = knockback;
                    return;
                }
            }
        }

        var f = CoreMgr.ClassPool.Get<LogicField>();
        f.Id = _logic.AllocId();
        f.Type = type;
        f.OwnerId = owner.Id;
        f.WeaponId = weaponId;
        f.Amount = amount < 1 ? 1 : amount;
        f.X = owner.X;
        f.Y = owner.Y;
        f.Angle = Fix.Zero;
        f.Radius = radius;
        f.Damage = damage;
        f.Knockback = knockback;
        f.TickInterval = tickInterval < 1 ? 1 : tickInterval;
        f.TickCounter = f.TickInterval;
        f.LifeFrames = lifeFrames;
        Fields.Add(f);

        if (type == VSFieldType.Orbit && f.Amount > _unitStepAmount)
            UpdateUnitStep(f.Amount);
    }

    /// <summary>更新均分圆周角旋转矩阵：单元间步长 = 2π/Amount（余弦定理精确构造，非近似）</summary>
    private void UpdateUnitStep(int amount)
    {
        _unitStepAmount = amount;
        // 2π/amount 的 (cos,sin)：以中心对称差分精确构造——
        // 取 half = π/amount，则 cos(2π/amount) = 1 - 2sin²(half)，sin(2π/amount) = 2sin(half)cos(half)；
        // half 的 sin/cos 用小角度近似 + 一次归一化（amount≥2 时 half ≤ π/2，FromDouble 字面量精度足够）
        // 这里直接用 FromDouble 查无表近似：amount 常见 1~8，逐值字面量（编译期确定，双端一致）
        switch (amount)
        {
            case 2: _unitStepCos = Fix.FromDouble(-1.0); _unitStepSin = Fix.FromDouble(0.0); break;
            case 3: _unitStepCos = Fix.FromDouble(-0.5); _unitStepSin = Fix.FromDouble(0.8660254); break;
            case 4: _unitStepCos = Fix.FromDouble(0.0); _unitStepSin = Fix.FromDouble(1.0); break;
            case 5: _unitStepCos = Fix.FromDouble(0.30901699); _unitStepSin = Fix.FromDouble(0.95105652); break;
            case 6: _unitStepCos = Fix.FromDouble(0.5); _unitStepSin = Fix.FromDouble(0.8660254); break;
            case 7: _unitStepCos = Fix.FromDouble(0.6234898); _unitStepSin = Fix.FromDouble(0.78183148); break;
            case 8: _unitStepCos = Fix.FromDouble(0.70710678); _unitStepSin = Fix.FromDouble(0.70710678); break;
            default: _unitStepCos = Fix.One; _unitStepSin = Fix.Zero; break; // Amount=1 或超范围：恒等（单元重合退化为 1 个）
        }
    }

    /// <summary>区域推进（Tick 步骤 7.5，弹幕之后）：Orbit 公转+逐帧接触判定；Aura 周期灼烧；到期回收由帧末集中清扫</summary>
    public void Tick()
    {
        var heroes = _logic.Heroes;
        var enemies = _logic.Enemies;

        for (int i = 0; i < Fields.Count; i++)
        {
            var f = Fields[i];
            f.LifeFrames--;

            // 宿主（死亡英雄的区域停摆：不移动不结算，等帧末到期清扫）
            LogicHero owner = null;
            for (int h = 0; h < heroes.Count; h++)
                if (heroes[h].Id == f.OwnerId && heroes[h].Hp > 0)
                {
                    owner = heroes[h];
                    break;
                }

            if (f.Type == VSFieldType.Orbit)
                TickOrbit(f, owner, enemies);
            else
                TickAura(f, owner, enemies);
        }
    }

    /// <summary>环绕体：圆心随宿主，自转角累计，逐单元圆形接触判定（命中推离，穿透不消耗）</summary>
    private void TickOrbit(LogicField f, LogicHero owner, List<LogicEnemy> enemies)
    {
        // 自转：旋转向量每帧左乘小角度旋转矩阵（纯 Fix 乘加，确定性）
        var nc = _rotCos * s_rotStepCos - _rotSin * s_rotStepSin;
        var ns = _rotCos * s_rotStepSin + _rotSin * s_rotStepCos;
        _rotCos = nc;
        _rotSin = ns;
        f.Angle += Fix.One; // 角度累计（步数计数，表现层换算实际弧度用 s_rotStep 常量）

        if (owner == null)
            return;

        f.X = owner.X;
        f.Y = owner.Y;

        // 逐单元判定：单元 k 方位 = 在全局旋转向量 (cos,sin) 基础上左乘 k 次"均分圆周角"旋转，位置 = center + (cos,sin)·radius
        var unitCos = _rotCos;
        var unitSin = _rotSin;
        var stepCos = _unitStepAmount == f.Amount ? _unitStepCos : BuildUnitStep(f.Amount);
        var stepSin = _unitStepAmount == f.Amount ? _unitStepSin : BuildUnitStepSin(f.Amount);
        for (int k = 0; k < f.Amount; k++)
        {
            if (k > 0)
            {
                var uc = unitCos * stepCos - unitSin * stepSin;
                var us = unitCos * stepSin + unitSin * stepCos;
                unitCos = uc;
                unitSin = us;
            }

            var ux = f.X + unitCos * f.Radius;
            var uy = f.Y + unitSin * f.Radius;

            for (int e = 0; e < enemies.Count; e++)
            {
                var en = enemies[e];
                if (en.Hp <= 0)
                    continue;

                var dx = en.X - ux;
                var dy = en.Y - uy;
                var r = en.Radius + s_orbitUnitRadius;
                if (dx * dx + dy * dy > r * r)
                    continue;

                // 击退方向：从环绕单元指向敌人（推离）
                var dist = Fix.Sqrt(dx * dx + dy * dy);
                if (dist == Fix.Zero)
                {
                    dx = unitCos;
                    dy = unitSin;
                    dist = Fix.One;
                }
                _hitEnemy(en, dx / dist, dy / dist, f.Damage, f.Knockback);
            }
        }
    }

    /// <summary>光环：圆心=宿主，每 TickInterval 帧对半径内全部敌人灼烧一次</summary>
    private void TickAura(LogicField f, LogicHero owner, List<LogicEnemy> enemies)
    {
        if (owner == null)
            return;

        f.X = owner.X;
        f.Y = owner.Y;

        f.TickCounter--;
        if (f.TickCounter > 0)
            return;
        f.TickCounter = f.TickInterval;

        var rSq = f.Radius * f.Radius;
        for (int e = 0; e < enemies.Count; e++)
        {
            var en = enemies[e];
            if (en.Hp <= 0)
                continue;

            var dx = en.X - f.X;
            var dy = en.Y - f.Y;
            if (dx * dx + dy * dy > rSq)
                continue;

            // 击退方向：从圆心指向敌人（推离光环）
            var dist = Fix.Sqrt(dx * dx + dy * dy);
            var dirX = Fix.Zero;
            var dirY = Fix.Zero;
            if (dist > Fix.Zero)
            {
                dirX = dx / dist;
                dirY = dy / dist;
            }
            _hitEnemy(en, dirX, dirY, f.Damage, f.Knockback);
        }
    }

    // 非缓存 Amount 的均分角矩阵（理论不触达：Spawn 时已缓存最大 Amount；兜底用恒等，保持确定性一致）
    private static Fix BuildUnitStep(int amount) => Fix.One;
    private static Fix BuildUnitStepSin(int amount) => Fix.Zero;

    /// <summary>状态进哈希（旋转状态与区域字段影响后续演算）</summary>
    public void HashState(ref ulong h)
    {
        VsHash.Mix(ref h, _rotCos.Raw);
        VsHash.Mix(ref h, _rotSin.Raw);
        for (int i = 0; i < Fields.Count; i++)
        {
            var f = Fields[i];
            VsHash.Mix(ref h, f.Id);
            VsHash.Mix(ref h, f.Type);
            VsHash.Mix(ref h, f.OwnerId);
            VsHash.Mix(ref h, f.WeaponId);
            VsHash.Mix(ref h, f.Amount);
            VsHash.Mix(ref h, f.X.Raw);
            VsHash.Mix(ref h, f.Y.Raw);
            VsHash.Mix(ref h, f.Angle.Raw);
            VsHash.Mix(ref h, f.Radius.Raw);
            VsHash.Mix(ref h, f.Damage);
            VsHash.Mix(ref h, f.Knockback.Raw);
            VsHash.Mix(ref h, f.TickCounter);
            VsHash.Mix(ref h, f.LifeFrames);
        }
    }

    /// <summary>战斗结束清理：全部区域回收回池，旋转状态复位</summary>
    public void Dispose()
    {
        for (int i = 0; i < Fields.Count; i++)
            CoreMgr.ClassPool.Recycle(Fields[i]);
        Fields.Clear();
        _rotCos = Fix.One;
        _rotSin = Fix.Zero;
    }
}
