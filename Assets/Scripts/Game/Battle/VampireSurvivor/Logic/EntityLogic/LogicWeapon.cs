/*--------------------------------------------------------------
 * File: LogicWeapon.cs
 * Author: Wsw
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using cfg;

/// <summary>
/// 武器实例（逻辑层）：等级/冷却计时/开火逻辑。范式(behavior)×目标策略(targetStrategy) 由表驱动，
/// 数值 = 武器等级表（TbVSWeaponLevel，id=武器id*100+等级）× 属性系统加成（2-2）。
/// 开火由 Logic 在 Tick 内固定序遍历（英雄序 → 武器序）驱动。
/// 范式（3-1 全量落地）：Projectile 弹道（直线散射/抛物线变体）、Instant 瞬时判定（弧形横扫）、
/// Area 持续区域（Orbit 环绕体 / Aura 光环，见 VsFieldSystem）。
/// 目标策略：Nearest 最近 / Random 随机 / Facing 面朝 / MoveDir 移动方向。
/// </summary>
public class LogicWeapon
{
    // ---- 手感数值（暂放常量区，频繁调整再挪表）----
    private static readonly Fix s_spreadHalfTan = Fix.FromDouble(0.131652); // 默认散射半步长 = tan(7.5°)（SpreadDeg=0 时兜底）
    private static readonly Fix s_deg2Rad = Fix.FromDouble(0.01745329);     // 角度→弧度（π/180）
    private static readonly Fix s_arcRange = Fix.FromDouble(2.2);           // 弧形横扫判定距离（单位，×Area 缩放）
    private static readonly Fix s_arcUnitRadius = Fix.FromDouble(0.5);      // 弧形横扫的扫掠线宽（判定用半宽）
    private static readonly Fix s_auraTickSec = Fix.FromDouble(0.5);        // 光环灼烧周期（秒）
    private static readonly Fix s_vertVelSec = Fix.FromDouble(7.5);         // 斧头上抛初速（单位/秒）
    private static readonly Fix s_landRadiusBase = Fix.FromDouble(0.9);     // 斧头落点判定半径（×Area 缩放）
    private static readonly Fix s_frameMsFix = Fix.FromInt(50);             // LogicFrameMs 的定点形式（构造期确定）

    public readonly int WeaponId;
    public readonly DVSWeapon Cfg; // 模板（范式/目标策略，只读）

    private DVSWeaponLevel _levelCfg; // 当前等级数值（升级时刷新）
    private int _level = 1;
    private int _cooldownFrames;      // 距下次开火剩余帧
    private readonly List<int> _hitMarks = new(); // 瞬时判定单次开火的本地去重（预分配复用，开火结束清空）

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

    /// <summary>每逻辑帧推进：冷却计时，就绪则按范式开火并重置冷却（无可用方向不消耗冷却，保持就绪）</summary>
    public void Tick(VampireLogic logic, LogicHero owner)
    {
        if (_cooldownFrames > 0)
        {
            _cooldownFrames--;
            return;
        }

        switch (Cfg.Behavior)
        {
            case VSWeaponBehavior.Projectile:
                FireProjectile(logic, owner);
                break;
            case VSWeaponBehavior.Instant:
                FireInstant(logic, owner);
                break;
            case VSWeaponBehavior.Area:
                FireArea(logic, owner);
                break;
        }

        _cooldownFrames = CalcCooldownFrames(owner);
    }

    // ---- 目标解析（Nearest/Random 产出索敌方向；Facing/MoveDir 直接产出朝向）----

    /// <summary>目标策略 → 单位方向向量（无可用方向返回 false：不消耗冷却）</summary>
    private bool ResolveAim(VampireLogic logic, LogicHero owner, out Fix dirX, out Fix dirY)
    {
        switch (Cfg.TargetStrategy)
        {
            case VSTargetStrategy.Nearest:
                var target = FindNearest(logic, owner);
                if (target == null)
                    return Fail(out dirX, out dirY);
                return NormalizeDir(target.X - owner.X, target.Y - owner.Y, out dirX, out dirY);

            case VSTargetStrategy.Random:
                var enemy = FindRandom(logic);
                if (enemy == null)
                    return Fail(out dirX, out dirY);
                return NormalizeDir(enemy.X - owner.X, enemy.Y - owner.Y, out dirX, out dirY);

            case VSTargetStrategy.Facing:
            case VSTargetStrategy.MoveDir:
                // 面朝方向（MoveDir 亦然：移动即更新面朝，静止沿用最后朝向，保证站桩仍有输出方向）
                dirX = owner.FacingX;
                dirY = owner.FacingY;
                return dirX != Fix.Zero || dirY != Fix.Zero;
        }

        return Fail(out dirX, out dirY);
    }

    private static bool Fail(out Fix dirX, out Fix dirY)
    {
        dirX = Fix.Zero;
        dirY = Fix.Zero;
        return false;
    }

    private static bool NormalizeDir(Fix dx, Fix dy, out Fix dirX, out Fix dirY)
    {
        var len = Fix.Sqrt(dx * dx + dy * dy);
        if (len == Fix.Zero)
        {
            dirX = Fix.One;
            dirY = Fix.Zero;
            return true; // 与目标重合：默认朝 +X
        }
        dirX = dx / len;
        dirY = dy / len;
        return true;
    }

    /// <summary>随机存活敌人（XRng 确定性：先数存活数再按序取第 n 个）</summary>
    private LogicEnemy FindRandom(VampireLogic logic)
    {
        var enemies = logic.Enemies;
        int alive = 0;
        for (int i = 0; i < enemies.Count; i++)
            if (enemies[i].Hp > 0)
                alive++;
        if (alive == 0)
            return null;

        var pick = logic.Rng.NextInt(alive);
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].Hp <= 0)
                continue;
            if (pick == 0)
                return enemies[i];
            pick--;
        }
        return null; // 不可达（alive 已核对）
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

    // ---- 公共数值换算 ----

    private long CalcDamage(LogicHero owner)
    {
        return (Fix.FromInt(_levelCfg.Damage) * (Fix.One + owner.Stats.Get(VSAttrType.Might))).Int;
    }

    private int CalcAmount(LogicHero owner)
    {
        return _levelCfg.Amount + owner.Stats.Get(VSAttrType.Amount).Int;
    }

    private static Fix SecToFrames(Fix sec)
    {
        return sec * Fix.FromInt(1000) / Fix.FromInt(VampireLogic.LogicFrameMs);
    }

    private static Fix PerSecToPerFrame(Fix perSec)
    {
        return perSec * Fix.FromInt(VampireLogic.LogicFrameMs) / Fix.FromInt(1000);
    }

    private static Fix PerSec2ToPerFrame2(Fix perSec2)
    {
        // 单位/秒² → 单位/帧²：× (frameMs/1000)²
        var k = Fix.FromInt(VampireLogic.LogicFrameMs) / Fix.FromInt(1000);
        return perSec2 * k * k;
    }

    /// <summary>冷却帧数：CooldownSec×(1-Cooldown 属性)，秒→帧向下取整，最少 1 帧防零冷却连发</summary>
    private int CalcCooldownFrames(LogicHero owner)
    {
        var cd = Fix.FromDouble(_levelCfg.CooldownSec) * (Fix.One - owner.Stats.Get(VSAttrType.Cooldown));
        var frames = SecToFrames(cd).Int;
        return frames < 1 ? 1 : frames;
    }

    // ---- 范式 1：弹道型（直线散射 / 抛物线变体）----

    /// <summary>
    /// 弹道开火（3-1 扩展）：Gravity&gt;0 走抛物线（斧头：朝索敌/面朝方向上抛，落地范围判定）；
    /// 否则直线弹（魔杖=Nearest 索敌单发；小刀=MoveDir 多向扇形，相邻弹夹角=SpreadDeg）。
    /// </summary>
    private void FireProjectile(VampireLogic logic, LogicHero owner)
    {
        if (!ResolveAim(logic, owner, out var dirX, out var dirY))
            return;

        var stats = owner.Stats;
        var damage = CalcDamage(owner);
        var amount = CalcAmount(owner);
        var life = SecToFrames(Fix.FromDouble(_levelCfg.DurationSec)).Int;
        var knockback = Fix.FromDouble(Cfg.Knockback);
        var speedMul = Fix.One + stats.Get(VSAttrType.ProjSpeed); // 弹速加成（3-3 被动）
        var areaMul = Fix.One + stats.Get(VSAttrType.Area);       // 范围加成（3-3 被动）

        // 抛物线变体（斧头）
        if (_levelCfg.Gravity > 0f)
        {
            var horizSpeed = PerSecToPerFrame(Fix.FromDouble(_levelCfg.ProjSpeed) * speedMul);
            var vertVel = PerSecToPerFrame(s_vertVelSec);
            var gravity = PerSec2ToPerFrame2(Fix.FromDouble(_levelCfg.Gravity));
            var landRadius = s_landRadiusBase * Fix.FromDouble(_levelCfg.Area) * areaMul;
            var landHits = 3 + amount; // 落点命中上限随弹数成长（纯手感）
            for (int i = 0; i < amount; i++)
            {
                ScatterDir(dirX, dirY, i, amount, s_spreadHalfTan, out var ax, out var ay);
                logic.SysProjectile.SpawnArc(owner, ax, ay, horizSpeed, vertVel, gravity,
                    damage, landHits, life, knockback, landRadius);
            }
            return;
        }

        // 直线弹
        var speed = PerSecToPerFrame(Fix.FromDouble(_levelCfg.ProjSpeed) * speedMul);
        var halfTan = SpreadHalfTan();
        for (int i = 0; i < amount; i++)
        {
            ScatterDir(dirX, dirY, i, amount, halfTan, out var ax, out var ay);
            logic.SysProjectile.Spawn(owner, ax, ay, speed, damage, _levelCfg.Pierce, life, knockback);
        }
    }

    /// <summary>扇形步长：表配 SpreadDeg&gt;0 用表值（相邻弹夹角一半的 tan），否则用默认 15°</summary>
    private Fix SpreadHalfTan()
    {
        if (_levelCfg.SpreadDeg <= 0f)
            return s_spreadHalfTan;
        return TanOfDeg(_levelCfg.SpreadDeg * 0.5);
    }

    /// <summary>中心对称散射方向：k = -m…0…+m，rot(d,θ) ∝ d + tan(θ)·perp（归一化精确等价旋转，免 sin/cos 表）</summary>
    private static void ScatterDir(Fix dirX, Fix dirY, int i, int amount, Fix halfTanStep, out Fix outX, out Fix outY)
    {
        var k = 2 * i - (amount - 1);
        var t = Fix.FromInt(k) * halfTanStep;
        var perpX = -dirY;
        var perpY = dirX;
        var rx = dirX + t * perpX;
        var ry = dirY + t * perpY;
        var rLen = Fix.Sqrt(rx * rx + ry * ry); // = √(1+t²) 恒 > 0
        outX = rx / rLen;
        outY = ry / rLen;
    }

    /// <summary>角度（度）→ tan 值：小角度（≤45°）用短 tan 近似（x·(1+x²/3)），大角度查少数字面量表（武器配置范围内）</summary>
    private static Fix TanOfDeg(double deg)
    {
        var rad = Fix.FromDouble(deg) * s_deg2Rad;
        // 常用角度字面量表（覆盖鞭子 60~130° 与扇形 16~22°；表外兜底 clamp 45°）
        if (deg > 45.0)
        {
            if (deg <= 55) return Fix.FromDouble(1.4281480);  // tan(55°)
            if (deg <= 60) return Fix.FromDouble(1.7320508);  // tan(60°)
            if (deg <= 65) return Fix.FromDouble(2.1445069);  // tan(65°)
            if (deg <= 70) return Fix.FromDouble(2.7474774);  // tan(70°)
            if (deg <= 75) return Fix.FromDouble(3.7320508);  // tan(75°)
            return Fix.FromDouble(5.6712818);                 // tan(80°+)（130° 半角 65° 已含）
        }

        // 小角度：tan(x) ≈ x + x³/3（x ≤ 0.785 rad 时误差 < 0.2%）
        var x2 = rad * rad;
        return rad + rad * x2 / Fix.FromInt(3);
    }

    // ---- 范式 2：瞬时判定型（弧形横扫，鞭子）----

    /// <summary>
    /// 瞬时弧形横扫（3-1 鞭子）：面朝方向张角 SpreadDeg 的弧带（距离 s_arcRange×Area，线宽 s_arcUnitRadius），
    /// 扫掠弧均匀采样做圆形判定（采样数 = 7 + Amount×2），同一次开火每个敌人至多命中一次（本地去重）。
    /// Amount 加成 = 伤害倍率 1+(Amount-1)×0.5（对齐 VS"多鞭"手感）。
    /// </summary>
    private void FireInstant(VampireLogic logic, LogicHero owner)
    {
        if (!ResolveAim(logic, owner, out var dirX, out var dirY))
            return;

        var amount = CalcAmount(owner);
        var dmg = (Fix.FromInt(_levelCfg.Damage) * (Fix.One + owner.Stats.Get(VSAttrType.Might))
                   * (Fix.One + Fix.FromInt(amount - 1) * Fix.Half)).Int; // 多鞭伤害倍率

        var range = s_arcRange * Fix.FromDouble(_levelCfg.Area) * (Fix.One + owner.Stats.Get(VSAttrType.Area)); // 范围加成（3-3 被动）
        var halfTan = TanOfDeg((_levelCfg.SpreadDeg > 0f ? _levelCfg.SpreadDeg : 60f) * 0.5);
        var samples = 7 + amount * 2;

        var enemies = logic.Enemies;
        var perpX = -dirY;
        var perpY = dirX;
        _hitMarks.Clear();
        for (int s = 0; s < samples; s++)
        {
            // 扫掠弧均匀采样：k 均分 [-halfTan, +halfTan]（中心对称），rot(d,θ) ∝ d + tan(θ)·perp
            var k = 2 * s - (samples - 1);
            var t = Fix.FromInt(k) * halfTan / Fix.FromInt(samples - 1);
            var rx = dirX + t * perpX;
            var ry = dirY + t * perpY;
            var rLen = Fix.Sqrt(rx * rx + ry * ry);
            var nx = rx / rLen;
            var ny = ry / rLen;
            var px = owner.X + nx * range;
            var py = owner.Y + ny * range;

            for (int e = 0; e < enemies.Count; e++)
            {
                var en = enemies[e];
                if (en.Hp <= 0 || IsAlreadyHit(en.Id))
                    continue;

                var dx = en.X - px;
                var dy = en.Y - py;
                var r = en.Radius + s_arcUnitRadius;
                if (dx * dx + dy * dy > r * r)
                    continue;

                _hitMarks.Add(en.Id); // 单次开火同敌只结算一次
                logic.SysProjectile.HitDirect(en, nx, ny, dmg, Fix.FromDouble(Cfg.Knockback));
            }
        }
        _hitMarks.Clear();
    }

    private bool IsAlreadyHit(int enemyId)
    {
        for (int i = 0; i < _hitMarks.Count; i++)
            if (_hitMarks[i] == enemyId)
                return true;
        return false;
    }

    // ---- 范式 3：持续区域型（环绕体/光环，圣经/大蒜）----

    /// <summary>
    /// 区域开火（3-1）：类型由等级表参数区分——OrbitRadius&gt;0 = 环绕体（圣经），否则 AuraRadius&gt;0 = 光环（大蒜）。
    /// Orbit：amount=环绕单元数，DurationSec=单周期时长（到期回收，冷却到点再生成）；
    /// Aura：常驻（系统内同武器去重，只刷新数值），灼烧周期 = s_auraTickSec。
    /// </summary>
    private void FireArea(VampireLogic logic, LogicHero owner)
    {
        var damage = CalcDamage(owner);
        var knockback = Fix.FromDouble(Cfg.Knockback);
        var areaMul = Fix.FromDouble(_levelCfg.Area) * (Fix.One + owner.Stats.Get(VSAttrType.Area)); // 表系数 × 范围加成（3-3 被动）

        if (_levelCfg.OrbitRadius > 0f)
        {
            // Orbit 环绕体（圣经）
            var radius = Fix.FromDouble(_levelCfg.OrbitRadius) * areaMul;
            var life = SecToFrames(Fix.FromDouble(_levelCfg.DurationSec)).Int;
            logic.SysField.Spawn(owner, VSFieldType.Orbit, WeaponId, radius, damage, knockback, 1, life, CalcAmount(owner));
        }
        else if (_levelCfg.AuraRadius > 0f)
        {
            // Aura 光环（大蒜）：常驻，同武器去重只刷新数值
            var radius = Fix.FromDouble(_levelCfg.AuraRadius) * areaMul;
            var tickFrames = SecToFrames(s_auraTickSec).Int;
            logic.SysField.Spawn(owner, VSFieldType.Aura, WeaponId, radius, damage, knockback, tickFrames, int.MaxValue, 1);
        }
    }
}
