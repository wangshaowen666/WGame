/*--------------------------------------------------------------
 * File: VampireLogic.cs
 * Author: Wsw
 * Time: 2026/08/31
 *--------------------------------------------------------------
 */

using System.Collections.Generic;

/// <summary>
/// 吸血鬼幸存者确定性逻辑层（纯 C#，零 UnityEngine 依赖）：
/// 与 TdLogic 同范式：有序 List + 标记死亡 + 每帧末集中清扫 + StateHash 逐帧校验。
/// 由 LocalDriver（单机）或 FrameSyncMgr（联机）以固定逻辑帧驱动，驱动侧对 Logic 零改动。
///
/// 确定性纪律（禁用）：
///   float/double（用 Fix）、UnityEngine.Random/System.Random（用 XRng + 固定种子）、
///   Time.deltaTime/Time.time（固定逻辑帧计数）、Physics（自研圆形碰撞）、
///   Dictionary 遍历 / LINQ / foreach 闭包（有序 List + for 循环）、
///   Tick 内 new / 装箱 / 字符串拼接（池化、预分配，Tick 零 GC 目标）。
/// </summary>
public class VampireLogic
{
    /// <summary>逻辑帧间隔（毫秒）。50ms = 20 帧/秒（联机时与服务器攒批对齐，2026-09-03 拍板），表现层以插值补足渲染帧率</summary>
    public const int LogicFrameMs = 50;

    // ---- 配置表 id（阶段 2-1 玩法数值全部入表：TbVSStage/TbVSCharacter/TbVSEnemy，刷怪敌人由 Stage 表 enemyId 配置）----
    // 公开供表现层反查表现实体 id（表现层读表派生，逻辑实体不携带表现字段）
    public const int StageId = 1;
    public const int HeroCfgId = 101001;

    // ---- 数值（构造时读表一次性转定点数，Tick 内零开销零 GC）----
    private readonly Fix _playerRadius;
    private readonly Fix _enemyMoveSpeed;
    private readonly Fix _enemyRadius;
    private readonly long _enemyHp;
    private readonly long _enemyExp; // 击杀掉落经验（敌人表 Exp）
    private readonly int _spawnIntervalFrames;
    private readonly int _spawnPerWave;
    private readonly Fix _spawnRadius;

    // ---- 击退参数（手感数值，暂放常量区，频繁调整再挪表）----
    private static readonly Fix s_knockbackDecay = Fix.FromDouble(0.8); // 每逻辑帧击退速度衰减系数（20Hz 语义）

    public bool GameOver { get; private set; }
    public int LastTickFrame { get; private set; }
    public long KillCount { get; private set; } // 弹幕击杀数（自爆不算，2-11 结算用）

    public readonly List<LogicHero> Heroes = new();
    public readonly List<LogicEnemy> Enemies = new();
    public readonly List<LogicProjectile> Projectiles = new();
    public readonly List<LogicDrop> Drops = new();

    /// <summary>
    /// 本帧命中事件（每次弹幕命中一条，含致死）：表现层每帧消费触发伤害飘字/命中特效；Tick 开头清上一帧事件。
    /// </summary>
    public readonly List<VsHitEvent> HitEvents = new();

    private readonly XRng _rng;
    private int _nextId = 1; // 实体稳定自增 ID（视图对账按此 ID 增删）

    public VampireLogic(long seed)
    {
        _rng = new XRng((ulong)seed);

        var stage = GameMgr.DataTable.TbVSStage.Get(StageId);
        var character = GameMgr.DataTable.TbVSCharacter.Get(HeroCfgId);
        var enemy = GameMgr.DataTable.TbVSEnemy.Get(stage.EnemyId);

        _playerRadius = Fix.FromDouble(character.Radius);
        _enemyMoveSpeed = Fix.FromDouble(enemy.MoveSpeed);
        _enemyRadius = Fix.FromDouble(enemy.Radius);
        _enemyHp = enemy.Hp;
        _enemyExp = enemy.Exp;
        _spawnIntervalFrames = (int)System.Math.Round(stage.SpawnIntervalSec * 1000 / LogicFrameMs);
        _spawnPerWave = stage.SpawnCountPerWave;
        _spawnRadius = Fix.FromDouble(stage.SpawnRadius);

        // 单机单人：创建 P1 玩家（出生在地图中心），属性系统接管 MaxHp/MoveSpeed（基础值来自角色表）
        var hero = new LogicHero
        {
            Id = _nextId++,
            HeroIndex = 0,
            Radius = _playerRadius,
        };
        hero.Stats = new LogicPlayerStats(hero, character);
        hero.Hp = hero.MaxHp; // 满血出生
        hero.Weapons.Add(new LogicWeapon(character.StartWeaponId)); // 初始武器（角色表）
        Heroes.Add(hero);
    }

    /// <summary>推进一帧（固定步长，帧号从 1 连续；inputs 可为空 = 空帧）</summary>
    public void Tick(int absFrame, IList<VsInput> inputs)
    {
        if (GameOver) return;
        LastTickFrame = absFrame;
        HitEvents.Clear(); // 清上一帧命中事件（OnFrame 在 Tick 后同步消费，不会跨帧残留）

        // 1. 应用输入：玩家移动
        for (int p = 0; p < Heroes.Count; p++)
            MovePlayer(Heroes[p], inputs);

        // 2. 玩家回复（Regen 属性，HP/秒；当前无被动恒 0 早退）
        for (int p = 0; p < Heroes.Count; p++)
            Heroes[p].Stats.ApplyRegen(LogicFrameMs);

        // 3. 武器开火（冷却计时 + 发射弹幕；英雄序 → 武器序固定遍历）
        for (int p = 0; p < Heroes.Count; p++)
        {
            var weapons = Heroes[p].Weapons;
            for (int w = 0; w < weapons.Count; w++)
                weapons[w].Tick(this, Heroes[p]);
        }

        // 4. 环形刷怪（按绝对帧号调度，确定性）
        if (absFrame % _spawnIntervalFrames == 0)
            SpawnEnemies();

        // 5. 敌人移动（追击最近存活玩家）
        for (int e = 0; e < Enemies.Count; e++)
            MoveEnemy(Enemies[e]);

        // 6. 自爆判定：接触玩家则敌人自爆死亡（玩家不扣血）
        for (int e = 0; e < Enemies.Count; e++)
            EnemySuicide(Enemies[e]);

        // 7. 弹幕飞行与命中判定
        TickProjectiles();

        // 8. 集中清扫：回收死亡敌人/失效弹幕/已生成掉落回池并移除（倒序 RemoveAt 保持存活者相对顺序，结果确定）
        for (int i = Enemies.Count - 1; i >= 0; i--)
        {
            if (Enemies[i].Hp <= 0)
            {
                CoreMgr.ClassPool.Recycle(Enemies[i]);
                Enemies.RemoveAt(i);
            }
        }

        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            var b = Projectiles[i];
            if (b.LifeFrames > 0 && b.Pierce > 0)
                continue;
            CoreMgr.ClassPool.Recycle(b);
            Projectiles.RemoveAt(i);
        }

        // 9. 失败判定：玩家全灭
        for (int h = 0; h < Heroes.Count; h++)
            if (Heroes[h].Hp <= 0)
            {
                GameOver = true;
                Log.Info("游戏结束");
                break;
            }
    }

    /// <summary>应用该玩家的输入方向移动，并更新面朝方向</summary>
    private void MovePlayer(LogicHero hero, IList<VsInput> inputs)
    {
        var dirX = Fix.Zero;
        var dirY = Fix.Zero;
        if (inputs != null)
            for (int i = 0; i < inputs.Count; i++)
                if (inputs[i].PlayerIndex == hero.HeroIndex)
                {
                    dirX = inputs[i].DirX;
                    dirY = inputs[i].DirY;
                    break;
                }

        if (dirX == Fix.Zero && dirY == Fix.Zero) return;

        hero.X += dirX * hero.MoveSpeed;
        hero.Y += dirY * hero.MoveSpeed;
        hero.FacingX = dirX;
        hero.FacingY = dirY;
    }

    /// <summary>环形刷怪：以最近玩家为圆心、视野外半径处的随机方向生成（随机方向向量代替角度，避免引入 sin/cos）</summary>
    private void SpawnEnemies()
    {
        var cx = Fix.Zero;
        var cy = Fix.Zero;
        if (Heroes.Count > 0)
        {
            cx = Heroes[0].X;
            cy = Heroes[0].Y;
        }

        for (int i = 0; i < _spawnPerWave; i++)
        {
            var dx = _rng.NextFix(-1, 1);
            var dy = _rng.NextFix(-1, 1);
            var lenSq = dx * dx + dy * dy;
            if (lenSq == Fix.Zero) continue; // 极小概率，跳过本只

            var len = Fix.Sqrt(lenSq);
            var enemy = CoreMgr.ClassPool.Get<LogicEnemy>(); // 池取（Recycle 时 Reset，字段必然干净）
            enemy.Id = _nextId++;
            enemy.X = cx + dx / len * _spawnRadius;
            enemy.Y = cy + dy / len * _spawnRadius;
            enemy.MoveSpeed = _enemyMoveSpeed;
            enemy.Radius = _enemyRadius;
            enemy.Hp = _enemyHp;
            Enemies.Add(enemy);
        }
    }

    /// <summary>敌人向最近存活玩家匀速移动（叠加击退位移与衰减），并更新面朝方向</summary>
    private void MoveEnemy(LogicEnemy en)
    {
        // 击退位移（冲量随帧衰减）：与追击位移叠加，被弹幕命中时被推离
        en.X += en.KnockbackX;
        en.Y += en.KnockbackY;
        en.KnockbackX = en.KnockbackX * s_knockbackDecay;
        en.KnockbackY = en.KnockbackY * s_knockbackDecay;

        LogicHero target = null;
        var bestSq = Fix.Zero;
        for (int h = 0; h < Heroes.Count; h++)
        {
            var hero = Heroes[h];
            if (hero.Hp <= 0) continue;
            var dx = hero.X - en.X;
            var dy = hero.Y - en.Y;
            var dSq = dx * dx + dy * dy;
            if (target == null || dSq < bestSq)
            {
                target = hero;
                bestSq = dSq;
            }
        }
        if (target == null) return;

        var tdx = target.X - en.X;
        var tdy = target.Y - en.Y;
        var dist = Fix.Sqrt(tdx * tdx + tdy * tdy);
        if (dist <= en.MoveSpeed)
        {
            en.X = target.X;
            en.Y = target.Y;
        }
        else
        {
            en.X += tdx / dist * en.MoveSpeed;
            en.Y += tdy / dist * en.MoveSpeed;
        }
        en.FacingX = tdx;
        en.FacingY = tdy;
    }

    /// <summary>发射弹幕（LogicWeapon 开火调用）：池取实体，从持有者当前位置沿单位方向射出</summary>
    public void SpawnProjectile(LogicHero owner, Fix dirX, Fix dirY, Fix speed, long damage, int pierce, int lifeFrames, Fix knockback)
    {
        var b = CoreMgr.ClassPool.Get<LogicProjectile>();
        b.Id = _nextId++;
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
        Projectiles.Add(b);
    }

    /// <summary>死亡掉落：敌人被弹幕击杀时生成经验宝石（价值 = 敌人表 Exp；磁吸/拾取 2-7）</summary>
    private void SpawnDrop(LogicEnemy en)
    {
        var drop = CoreMgr.ClassPool.Get<LogicDrop>();
        drop.Id = _nextId++;
        drop.X = en.X;
        drop.Y = en.Y;
        drop.Value = _enemyExp;
        Drops.Add(drop);
    }

    /// <summary>弹幕飞行与命中：直线推进 + 圆形点判定（距敌圆心 ≤ 敌半径，MVP 暴力 O(n×m)，阶段 7-1 空间网格）</summary>
    private void TickProjectiles()
    {
        for (int i = 0; i < Projectiles.Count; i++)
        {
            var b = Projectiles[i];
            b.X += b.DirX * b.MoveSpeed;
            b.Y += b.DirY * b.MoveSpeed;
            b.LifeFrames--;

            for (int e = 0; e < Enemies.Count; e++)
            {
                var en = Enemies[e];
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
                    KillCount++;
                    SpawnDrop(en); // 死亡掉落（经验宝石，逻辑实体；磁吸/拾取 2-7）
                }
                en.LastHitFrame = LastTickFrame; // 受击感知（表现层据此触发闪白）
                HitEvents.Add(new VsHitEvent(en.X, en.Y, b.Damage)); // 命中事件（表现层：飘字+特效）
                b.LastHitEnemyId = en.Id;
                b.Pierce--;
                break; // 每帧每弹最多命中一个敌人
            }
        }
    }

    /// <summary>自爆：进入碰撞半径则敌人自爆（Hp=0 交由帧末集中清扫），玩家不扣血</summary>
    private void EnemySuicide(LogicEnemy en)
    {
        for (int h = 0; h < Heroes.Count; h++)
        {
            var hero = Heroes[h];
            if (hero.Hp <= 0) continue;
            var r = en.Radius + hero.Radius;
            var dx = hero.X - en.X;
            var dy = hero.Y - en.Y;
            if (dx * dx + dy * dy <= r * r)
            {
                en.Hp = 0;
                return;
            }
        }
    }

    /// <summary>全量状态哈希（FNV-1a 64）：复现/联机时逐帧对比验证确定性</summary>
    public ulong StateHash()
    {
        var h = 14695981039346656037UL;
        Mix(ref h, LastTickFrame);
        Mix(ref h, GameOver ? 1 : 0);
        for (int i = 0; i < Heroes.Count; i++)
        {
            var p = Heroes[i];
            Mix(ref h, p.Id);
            Mix(ref h, p.HeroIndex);
            Mix(ref h, p.X.Raw);
            Mix(ref h, p.Y.Raw);
            Mix(ref h, p.FacingX.Raw);
            Mix(ref h, p.FacingY.Raw);
            Mix(ref h, p.Hp);
        }
        for (int i = 0; i < Enemies.Count; i++)
        {
            var e = Enemies[i];
            Mix(ref h, e.Id);
            Mix(ref h, e.X.Raw);
            Mix(ref h, e.Y.Raw);
            Mix(ref h, e.FacingX.Raw);
            Mix(ref h, e.FacingY.Raw);
            Mix(ref h, e.KnockbackX.Raw);
            Mix(ref h, e.KnockbackY.Raw);
            Mix(ref h, e.Hp);
        }
        for (int i = 0; i < Projectiles.Count; i++)
        {
            var b = Projectiles[i];
            Mix(ref h, b.Id);
            Mix(ref h, b.X.Raw);
            Mix(ref h, b.Y.Raw);
            Mix(ref h, b.DirX.Raw);
            Mix(ref h, b.DirY.Raw);
            Mix(ref h, b.MoveSpeed.Raw);
            Mix(ref h, b.Damage);
            Mix(ref h, b.Pierce);
            Mix(ref h, b.LifeFrames);
            Mix(ref h, b.LastHitEnemyId);
            Mix(ref h, b.Knockback.Raw);
        }
        for (int i = 0; i < Drops.Count; i++)
        {
            var d = Drops[i];
            Mix(ref h, d.Id);
            Mix(ref h, d.X.Raw);
            Mix(ref h, d.Y.Raw);
            Mix(ref h, d.Value);
        }
        return h;
    }

    /// <summary>战斗结束清理逻辑层资源：存活敌人/弹幕/掉落全部回收回池，复用跨局</summary>
    public void Dispose()
    {
        for (int i = 0; i < Enemies.Count; i++)
            CoreMgr.ClassPool.Recycle(Enemies[i]);
        for (int i = 0; i < Projectiles.Count; i++)
            CoreMgr.ClassPool.Recycle(Projectiles[i]);
        for (int i = 0; i < Drops.Count; i++)
            CoreMgr.ClassPool.Recycle(Drops[i]);
        Enemies.Clear();
        Projectiles.Clear();
        Drops.Clear();
        Heroes.Clear();
    }

    private static void Mix(ref ulong h, long v)
    {
        h = (h ^ (ulong)v) * 1099511628211UL;
    }
}

/// <summary>玩家实体（逻辑层）：位置/面朝/移动/血量，Tick 内由输入驱动移动；属性经 Stats 结算写回</summary>
public class LogicHero
{
    public int Id;
    public int HeroIndex;
    public Fix X, Y;
    public Fix FacingX, FacingY; // 当前移动方向
    public Fix MoveSpeed;
    public Fix Radius;           // 碰撞半径
    public long MaxHp;
    public long Hp;
    public LogicPlayerStats Stats; // 属性系统（Logic 构造时装配）
    public readonly List<LogicWeapon> Weapons = new(); // 持有武器（升级选牌增改，2-8）
}

/// <summary>敌人实体（逻辑层）：追击最近玩家，接触玩家即自爆死亡（Hp<=0 由每帧末集中清扫回收）。</summary>
/// <remarks>实现 IResetable 供 ClassPool 池化复用（Recycle 时 Reset 全清，避免脏数据）</remarks>
public class LogicEnemy : IResetable
{
    public int Id;
    public int LastHitFrame;     // 最后被弹幕命中的逻辑帧（受击感知，表现向：不进 StateHash，播闪白用）
    public Fix X, Y;
    public Fix FacingX, FacingY; // 追击方向（表现层翻转用）
    public Fix MoveSpeed;
    public Fix Radius;           // 碰撞半径
    public Fix KnockbackX, KnockbackY; // 击退冲量（每帧位移并衰减，进 StateHash——影响位置演算）
    public long Hp;

    public void Reset()
    {
        Id = 0;
        LastHitFrame = 0;
        X = Fix.Zero;
        Y = Fix.Zero;
        FacingX = Fix.Zero;
        FacingY = Fix.Zero;
        MoveSpeed = Fix.Zero;
        Radius = Fix.Zero;
        KnockbackX = Fix.Zero;
        KnockbackY = Fix.Zero;
        Hp = 0;
    }
}

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
