/*--------------------------------------------------------------
 * File: VampireLogic.cs
 * Author: Wsw
 * Time: 2026/08/31
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using cfg;

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

    // ---- 配置表 id（阶段 2-1 玩法数值全部入表：TbVSStage/TbVSWave/TbVSCharacter/TbVSEnemy，刷怪由波次表按分钟段时间轴驱动）----
    // 公开供表现层反查表现实体 id（表现层读表派生，逻辑实体不携带表现字段）
    public const int StageId = 1;
    public const int HeroCfgId = 101001;

    // ---- 数值（构造时读表一次性转定点数，Tick 内零开销零 GC）----
    private readonly Fix _playerRadius;
    private readonly Fix _spawnRadius;
    private readonly int _expBase; // 升级经验曲线基数（1级->2级所需，关卡表）
    private readonly int _expStep; // 升级经验曲线步长（每级递增，关卡表）

    /// <summary>
    /// 波次行运行时状态（2-9 波次调度，构造期预计算，Tick 零 GC）：
    /// 每行 = 自 StartMinute 起持续刷怪的一条流（每 IntervalFrames 刷 PerWave 只，直到战斗结束）；
    /// 同分钟段可配多行混刷多种敌人；强度成长 = 波次表系数倍乘敌表基础值（血量/移速），
    /// 单机关卡与联机无尽共用（无尽=持续加配更多行，无上限曲线阶段 5-6）。
    /// </summary>
    private sealed class WaveRow
    {
        public long NextSpawnFrame; // 下次刷怪帧（初始=生效起始帧，运行时推进）
        public int IntervalFrames;  // 刷怪间隔（帧，≥1）
        public int PerWave;         // 每批数量
        public int CfgId;           // 敌种（#VSEnemy）
        public long Hp;             // 敌表基础血量 × HpMul
        public Fix MoveSpeed;       // 敌表基础移速 × SpeedMul
        public Fix Radius;          // 碰撞半径（敌表基础）
        public long Exp;            // 击杀经验（敌表基础，经验不随时间膨胀：成长靠敌种换档）
    }

    private readonly List<WaveRow> _waves = new(); // 本关波次行（按生效时间稳定排序）

    // ---- 击退参数（手感数值，暂放常量区，频繁调整再挪表）----
    private static readonly Fix s_knockbackDecay = Fix.FromDouble(0.8); // 每逻辑帧击退速度衰减系数（20Hz 语义）
    private static readonly Fix s_dropAttractSpeed = Fix.FromDouble(0.6); // 掉落物磁吸位移速度（单位/逻辑帧）

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

    // ---- 升级选牌（2-8）：升级次数入队，逐组三选一；表现层暂停驱动后经 ApplyChoice 消费 ----
    /// <summary>每次升级可选的强化项数量</summary>
    public const int ChoiceCount = 3;
    /// <summary>待选择组数（含当前展示组；连升多级逐组选择）</summary>
    public int PendingLevelUpCount { get; private set; }
    /// <summary>当前待选的强化项（三选一；空 = 无待选择）</summary>
    public readonly List<VsLevelUpChoice> CurrentChoices = new();
    /// <summary>是否有待选择的强化项（表现层据此暂停并弹出选牌面板）</summary>
    public bool HasPendingChoice => CurrentChoices.Count > 0;

    private readonly List<VsLevelUpChoice> _choicePool = new(); // 抽选缓冲（DrawChoices 复用，零分配）

    private readonly XRng _rng;
    private int _nextId = 1; // 实体稳定自增 ID（视图对账按此 ID 增删）

    public VampireLogic(long seed)
    {
        _rng = new XRng((ulong)seed);

        var stage = GameMgr.DataTable.TbVSStage.Get(StageId);
        var character = GameMgr.DataTable.TbVSCharacter.Get(HeroCfgId);

        _playerRadius = Fix.FromDouble(character.Radius);
        _spawnRadius = Fix.FromDouble(stage.SpawnRadius);
        _expBase = stage.ExpBase;
        _expStep = stage.ExpStep;

        BuildWaves(StageId);

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
        hero.XpToNext = CalcXpToNext(hero.Level);
        hero.Weapons.Add(new LogicWeapon(character.StartWeaponId)); // 初始武器（角色表）
        Heroes.Add(hero);
    }

    /// <summary>
    /// 构建本关波次行（2-9）：过滤波次表本关行，敌表基础值 × 波次系数预计算为定点/整数；
    /// 按生效起始帧稳定排序（同帧保持表行序），保证刷怪序列确定性。
    /// </summary>
    private void BuildWaves(int stageId)
    {
        var rows = GameMgr.DataTable.TbVSWave.DataList;
        for (int i = 0; i < rows.Count; i++)
        {
            var cfg = rows[i];
            if (cfg.StageId != stageId)
                continue;

            var enemy = GameMgr.DataTable.TbVSEnemy.Get(cfg.EnemyId);
            _waves.Add(new WaveRow
            {
                NextSpawnFrame = (long)cfg.StartMinute * 60000 / LogicFrameMs, // 分钟 → 逻辑帧
                IntervalFrames = System.Math.Max(1, (int)System.Math.Round(cfg.SpawnIntervalSec * 1000 / LogicFrameMs)),
                PerWave = cfg.SpawnCountPerWave,
                CfgId = enemy.Id,
                Hp = (Fix.FromInt(enemy.Hp) * Fix.FromDouble(cfg.HpMul)).Int,
                MoveSpeed = Fix.FromDouble(enemy.MoveSpeed) * Fix.FromDouble(cfg.SpeedMul),
                Radius = Fix.FromDouble(enemy.Radius),
                Exp = enemy.Exp,
            });
        }

        // 插入排序（稳定）：按生效起始帧升序
        for (int i = 1; i < _waves.Count; i++)
        {
            var row = _waves[i];
            var j = i - 1;
            while (j >= 0 && _waves[j].NextSpawnFrame > row.NextSpawnFrame)
            {
                _waves[j + 1] = _waves[j];
                j--;
            }
            _waves[j + 1] = row;
        }

        if (_waves.Count == 0)
            Log.Error("[吸血鬼] 波次表无本关行，本局不会刷怪，检查 #VSWave.xlsx stageId:", stageId);
    }

    /// <summary>推进一帧（固定步长，帧号从 1 连续；inputs 可为空 = 空帧）</summary>
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
        TickWaves(absFrame);

        // 5. 敌人移动（追击最近存活玩家）
        for (int e = 0; e < Enemies.Count; e++)
            MoveEnemy(Enemies[e]);

        // 6. 自爆判定：接触玩家则敌人自爆死亡（玩家不扣血）
        for (int e = 0; e < Enemies.Count; e++)
            EnemySuicide(Enemies[e]);

        // 7. 弹幕飞行与命中判定
        TickProjectiles();

        // 8. 掉落物磁吸与拾取（经验入账 + 升级结算）
        TickDrops();

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

    /// <summary>波次推进：到期波次行各刷一批（按行序遍历，确定性）</summary>
    private void TickWaves(int absFrame)
    {
        for (int i = 0; i < _waves.Count; i++)
        {
            var row = _waves[i];
            if (absFrame < row.NextSpawnFrame)
                continue;
            SpawnEnemies(row);
            row.NextSpawnFrame += row.IntervalFrames;
        }
    }

    /// <summary>环形刷一批：以最近玩家为圆心、视野外半径处的随机方向生成（随机方向向量代替角度，避免引入 sin/cos）</summary>
    private void SpawnEnemies(WaveRow row)
    {
        var cx = Fix.Zero;
        var cy = Fix.Zero;
        if (Heroes.Count > 0)
        {
            cx = Heroes[0].X;
            cy = Heroes[0].Y;
        }

        for (int i = 0; i < row.PerWave; i++)
        {
            var dx = _rng.NextFix(-1, 1);
            var dy = _rng.NextFix(-1, 1);
            var lenSq = dx * dx + dy * dy;
            if (lenSq == Fix.Zero) continue; // 极小概率，跳过本只

            var len = Fix.Sqrt(lenSq);
            var enemy = CoreMgr.ClassPool.Get<LogicEnemy>(); // 池取（Recycle 时 Reset，字段必然干净）
            enemy.Id = _nextId++;
            enemy.CfgId = row.CfgId;
            enemy.X = cx + dx / len * _spawnRadius;
            enemy.Y = cy + dy / len * _spawnRadius;
            enemy.MoveSpeed = row.MoveSpeed;
            enemy.Radius = row.Radius;
            enemy.Hp = row.Hp;
            enemy.Exp = row.Exp;
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

    /// <summary>死亡掉落：敌人被弹幕击杀时生成经验宝石（IResetable 池化；价值=敌种 Exp，敌种间自然分档，视觉分档 7-3 治理时再配）</summary>
    private void SpawnDrop(LogicEnemy en)
    {
        var drop = CoreMgr.ClassPool.Get<LogicDrop>();
        drop.Id = _nextId++;
        drop.X = en.X;
        drop.Y = en.Y;
        drop.Value = en.Exp;
        Drops.Add(drop);
    }

    /// <summary>
    /// 掉落物磁吸与拾取（2-7）：以最近存活英雄为参照——
    /// 进入拾取半径（英雄碰撞半径）即拾取入账经验并回池；进入磁吸半径则向英雄移动（吸引速度＞玩家移速，必然追上）；
    /// 两半径外静止躺尸待捡。
    /// </summary>
    private void TickDrops()
    {
        for (int i = Drops.Count - 1; i >= 0; i--) // 倒序：拾取即回池移除
        {
            var drop = Drops[i];

            // 最近存活英雄（联机双人时磁吸/拾取归属最近者）
            LogicHero target = null;
            var bestSq = Fix.Zero;
            for (int h = 0; h < Heroes.Count; h++)
            {
                var hero = Heroes[h];
                if (hero.Hp <= 0) continue;
                var dx = hero.X - drop.X;
                var dy = hero.Y - drop.Y;
                var dSq = dx * dx + dy * dy;
                if (target == null || dSq < bestSq)
                {
                    target = hero;
                    bestSq = dSq;
                }
            }
            if (target == null) return; // 无存活英雄：掉落全部静止（帧末 GameOver 判定兜底）

            var pickupR = target.Radius;
            if (bestSq <= pickupR * pickupR)
            {
                GainXp(target, drop.Value);
                CoreMgr.ClassPool.Recycle(drop);
                Drops.RemoveAt(i);
                continue;
            }

            var magnetR = target.MagnetRadius;
            if (bestSq > magnetR * magnetR)
                continue;

            // 磁吸：朝英雄移动，步长不超过剩余距离（防越过目标抖动；此处 dist＞拾取半径＞0，除法安全）
            var dist = Fix.Sqrt(bestSq);
            var step = dist < s_dropAttractSpeed ? dist : s_dropAttractSpeed;
            drop.X += (target.X - drop.X) / dist * step;
            drop.Y += (target.Y - drop.Y) / dist * step;
        }
    }

    /// <summary>经验入账并结算升级：获取量 ×(1+Growth 成长属性)；可连升多级，每级产出一条升级事件（2-8 选牌消费）</summary>
    private void GainXp(LogicHero hero, long value)
    {
        var gain = (Fix.FromInt((int)value) * (Fix.One + hero.Stats.Get(VSAttrType.Growth))).Int;
        hero.Xp += gain;
        while (hero.Xp >= hero.XpToNext)
        {
            hero.Xp -= hero.XpToNext;
            hero.Level++;
            hero.XpToNext = CalcXpToNext(hero.Level);
            LevelUpEvents.Add(new VsLevelUpEvent(hero.Id, hero.Level));

            // 升级次数入队；无待展示组则立即抽选（已有组展示中则其应用后再抽，保证池状态最新）
            PendingLevelUpCount++;
            if (CurrentChoices.Count == 0)
                DrawChoices();
        }
    }

    /// <summary>升级经验曲线（关卡表线性）：n 级升至 n+1 所需 = ExpBase + ExpStep×(n-1)</summary>
    private long CalcXpToNext(int level)
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
            var j = i + _rng.NextInt(_choicePool.Count - i);
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
        var hero = Heroes[0];

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

        var hero = Heroes[0];
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
            Mix(ref h, p.Level);
            Mix(ref h, p.Xp);
            Mix(ref h, p.XpToNext);
        }
        Mix(ref h, PendingLevelUpCount); // 选牌状态进哈希（应用选择影响后续演算）
        for (int i = 0; i < CurrentChoices.Count; i++)
        {
            Mix(ref h, CurrentChoices[i].IsWeapon ? 1 : 0);
            Mix(ref h, CurrentChoices[i].ItemId);
        }
        for (int i = 0; i < Enemies.Count; i++)
        {
            var e = Enemies[i];
            Mix(ref h, e.Id);
            Mix(ref h, e.CfgId);
            Mix(ref h, e.X.Raw);
            Mix(ref h, e.Y.Raw);
            Mix(ref h, e.FacingX.Raw);
            Mix(ref h, e.FacingY.Raw);
            Mix(ref h, e.KnockbackX.Raw);
            Mix(ref h, e.KnockbackY.Raw);
            Mix(ref h, e.Hp);
            Mix(ref h, e.Exp);
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
    public Fix Radius;           // 碰撞半径（兼作经验宝石拾取半径）
    public Fix MagnetRadius;     // 磁吸半径（Stats 结算写回：角色表基础 × (1+Magnet 被动)）
    public long MaxHp;
    public long Hp;
    public int Level;            // 当前等级（1 起，拾取经验升级，2-7）
    public long Xp;              // 距下一级已累积经验
    public long XpToNext;        // 升至下一级所需经验（关卡表线性曲线）
    public LogicPlayerStats Stats; // 属性系统（Logic 构造时装配）
    public readonly List<LogicWeapon> Weapons = new(); // 持有武器（升级选牌增改，2-8）

    /// <summary>查找已持有武器（未持有返回 null）</summary>
    public LogicWeapon FindWeapon(int weaponId)
    {
        for (int i = 0; i < Weapons.Count; i++)
            if (Weapons[i].WeaponId == weaponId)
                return Weapons[i];
        return null;
    }
}

/// <summary>敌人实体（逻辑层）：追击最近玩家，接触玩家即自爆死亡（Hp<=0 由每帧末集中清扫回收）。</summary>
/// <remarks>实现 IResetable 供 ClassPool 池化复用（Recycle 时 Reset 全清，避免脏数据）</remarks>
public class LogicEnemy : IResetable
{
    public int Id;
    public int CfgId;            // 敌种配置 id（#VSEnemy，逻辑身份；表现层据此反查表现实体/掉落）
    public int LastHitFrame;     // 最后被弹幕命中的逻辑帧（受击感知，表现向：不进 StateHash，播闪白用）
    public Fix X, Y;
    public Fix FacingX, FacingY; // 追击方向（表现层翻转用）
    public Fix MoveSpeed;
    public Fix Radius;           // 碰撞半径
    public Fix KnockbackX, KnockbackY; // 击退冲量（每帧位移并衰减，进 StateHash——影响位置演算）
    public long Hp;
    public long Exp;             // 击杀掉落经验（敌表基础值，波次系数不作用于经验）

    public void Reset()
    {
        Id = 0;
        CfgId = 0;
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
        Exp = 0;
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
