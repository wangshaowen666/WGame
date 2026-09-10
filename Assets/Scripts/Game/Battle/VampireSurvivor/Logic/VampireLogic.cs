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
/// 本类只做 Tick 编排与公共状态持有（实体列表/事件/ID/随机源），玩法机制拆分在 Logic/Systems/ 下：
/// 波次 VsWaveSystem、敌人 VsEnemySystem、弹幕 VsProjectileSystem、掉落 VsDropSystem、升级选牌 VsLevelUpSystem。
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

    // ---- 配置表 id（阶段 2-1 玩法数值全部入表：TbVSStage/TbVSWave/TbVSCharacter/TbVSEnemy，刷怪由波次表按分钟段时间轴驱动）----
    // 公开供表现层反查表现实体 id（表现层读表派生，逻辑实体不携带表现字段）
    public const int StageId = 1;
    public const int HeroCfgId = 101001;

    // ---- 玩法系统（构造期装配；Tick 按固定序驱动，系统间交互经本类公共状态/系统引用显式调用）----
    public readonly VsWaveSystem SysWave;             // 波次刷怪（2-9）
    public readonly VsEnemySystem SysEnemy;           // 敌人移动/自爆
    public readonly VsProjectileSystem SysProjectile; // 弹幕飞行/命中
    public readonly VsDropSystem SysDrop;             // 掉落磁吸/拾取（2-7）
    public readonly VsLevelUpSystem SysLevelUp;       // 经验/升级选牌（2-8）

    private readonly Fix _playerRadius;

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

    /// <summary>
    /// 本帧升级事件（拾取经验触发，一帧内连升多条）：表现层消费（2-8 选牌面板暂停 / 2-10 HUD）；Tick 开头清上一帧事件。
    /// </summary>
    public readonly List<VsLevelUpEvent> LevelUpEvents = new();

    private readonly XRng _rng;
    private int _nextId = 1; // 实体稳定自增 ID（视图对账按此 ID 增删）

    public VampireLogic(long seed)
    {
        _rng = new XRng((ulong)seed);

        var character = GameMgr.DataTable.TbVSCharacter.Get(HeroCfgId);
        _playerRadius = Fix.FromDouble(character.Radius);

        // 系统装配：各系统构造期读各自表数据（波次/关卡曲线），Tick 内零分配
        SysWave = new VsWaveSystem(this, StageId);
        SysEnemy = new VsEnemySystem(this);
        SysProjectile = new VsProjectileSystem(this);
        SysDrop = new VsDropSystem(this);
        SysLevelUp = new VsLevelUpSystem(this, StageId);

        // 单机单人：创建 P1 玩家（出生在地图中心），属性系统接管 MaxHp/MoveSpeed/MagnetRadius（基础值来自角色表）
        var hero = new LogicHero
        {
            Id = _nextId++,
            HeroIndex = 0,
            Radius = _playerRadius,
        };
        hero.Stats = new LogicPlayerStats(hero, character);
        hero.Hp = hero.MaxHp; // 满血出生
        hero.Level = 1;
        hero.XpToNext = SysLevelUp.CalcXpToNext(hero.Level);
        hero.Weapons.Add(new LogicWeapon(character.StartWeaponId)); // 初始武器（角色表）
        Heroes.Add(hero);
    }

    /// <summary>分配实体稳定自增 ID（各系统生成实体时调用，保证全局唯一且确定性）</summary>
    public int AllocId() => _nextId++;

    /// <summary>确定性随机源（波次刷怪/升级抽选共用同一序列，保证全局确定）</summary>
    public XRng Rng => _rng;

    /// <summary>击杀数入账（弹幕致死时由弹幕系统调用）</summary>
    public void AddKill() => KillCount++;

    /// <summary>
    /// 推进一帧（固定步长，帧号从 1 连续；inputs 可为空 = 空帧）。
    /// 步骤顺序即确定性本身：调整任意两步顺序都会改变演算结果与 StateHash。
    /// </summary>
    public void Tick(int absFrame, IList<VsInput> inputs)
    {
        if (GameOver) return;
        LastTickFrame = absFrame;
        HitEvents.Clear(); // 清上一帧命中事件（OnFrame 在 Tick 后同步消费，不会跨帧残留）
        LevelUpEvents.Clear(); // 清上一帧升级事件

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

        // 4. 波次刷怪（分钟段时间轴驱动，按绝对帧号调度，确定性）
        SysWave.Tick(absFrame);

        // 5. 敌人移动（追击最近存活玩家）
        SysEnemy.TickMove();

        // 6. 自爆判定：接触玩家则敌人自爆死亡（玩家不扣血）
        SysEnemy.TickSuicide();

        // 7. 弹幕飞行与命中判定
        SysProjectile.Tick();

        // 8. 掉落物磁吸与拾取（经验入账 + 升级结算）
        SysDrop.Tick();

        // 9. 集中清扫：回收死亡敌人/失效弹幕回池并移除（倒序 RemoveAt 保持存活者相对顺序，结果确定）
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

        // 10. 失败判定：玩家全灭
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

    /// <summary>
    /// 全量状态哈希（FNV-1a 64）：复现/联机时逐帧对比验证确定性。
    /// 各系统在既有 Mix 序列尾部追加自己的实体字段；禁止重排/插删既有字段，哈希值须逐帧稳定。
    /// </summary>
    public ulong StateHash()
    {
        var h = 14695981039346656037UL;
        VsHash.Mix(ref h, LastTickFrame);
        VsHash.Mix(ref h, GameOver ? 1 : 0);
        for (int i = 0; i < Heroes.Count; i++)
        {
            var p = Heroes[i];
            VsHash.Mix(ref h, p.Id);
            VsHash.Mix(ref h, p.HeroIndex);
            VsHash.Mix(ref h, p.X.Raw);
            VsHash.Mix(ref h, p.Y.Raw);
            VsHash.Mix(ref h, p.FacingX.Raw);
            VsHash.Mix(ref h, p.FacingY.Raw);
            VsHash.Mix(ref h, p.Hp);
            VsHash.Mix(ref h, p.Level);
            VsHash.Mix(ref h, p.Xp);
            VsHash.Mix(ref h, p.XpToNext);
        }
        SysLevelUp.HashState(ref h); // 选牌状态进哈希（应用选择影响后续演算）
        SysEnemy.HashState(ref h);
        SysProjectile.HashState(ref h);
        SysDrop.HashState(ref h);
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
}
