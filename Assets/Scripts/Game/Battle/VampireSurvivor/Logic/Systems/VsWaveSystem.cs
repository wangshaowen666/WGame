/*--------------------------------------------------------------
 * File: VsWaveSystem.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

using System.Collections.Generic;

/// <summary>
/// 波次刷怪系统（2-9，纯 C# 确定性）：分钟段时间轴驱动，每行 = 自 StartMinute 起持续刷怪的一条流
/// （每 IntervalFrames 刷 PerWave 只，直到战斗结束）；同分钟段可配多行混刷多种敌人；
/// 强度成长 = 波次表系数倍乘敌表基础值（血量/移速），单机关卡与联机无尽共用（无尽=持续加配更多行，无上限曲线阶段 5-6）。
/// </summary>
public sealed class VsWaveSystem
{
    /// <summary>波次行运行时状态（构造期预计算，Tick 零 GC）</summary>
    private sealed class WaveRow
    {
        public long NextSpawnFrame; // 下次刷怪帧（初始=生效起始帧，运行时推进）
        public int IntervalFrames;  // 刷怪间隔（帧，≥1）
        public int PerWave;         // 每批数量
        public int CfgId;           // 敌种（#VSEnemy）
        public int Behavior;        // 行为类型（敌表 Behavior，3-4：追击/直线横穿）
        public long Hp;             // 敌表基础血量 × HpMul
        public Fix MoveSpeed;       // 敌表基础移速 × SpeedMul
        public Fix Radius;          // 碰撞半径（敌表基础 × ViewScale，3-5 精英放大）
        public long Damage;         // 碰撞伤害（敌表基础，碰到英雄时扣除；0=纯自爆不扣血）
        public long Exp;            // 击杀经验（敌表基础，经验不随时间膨胀：成长靠敌种换档）
        public bool IsElite;        // 精英行（3-5：到点一次性刷一批，死亡掉宝箱）
        public bool Done;           // 精英行已刷出标记（一次性；普通行不使用）
    }

    private readonly VampireLogic _logic;
    private readonly List<WaveRow> _waves = new(); // 本关波次行（按生效时间稳定排序）
    private readonly Fix _spawnRadius;

    public VsWaveSystem(VampireLogic logic, int stageId)
    {
        _logic = logic;
        _spawnRadius = Fix.FromDouble(GameMgr.DataTable.TbVSStage.Get(stageId).SpawnRadius);
        BuildWaves(stageId);
    }

    /// <summary>波次推进：到期波次行各刷一批（按行序遍历，确定性）。精英行只刷一次（3-5）。由 VampireLogic.Tick 步骤 4 调用</summary>
    public void Tick(int absFrame)
    {
        for (int i = 0; i < _waves.Count; i++)
        {
            var row = _waves[i];
            if (row.Done || absFrame < row.NextSpawnFrame)
                continue;
            SpawnEnemies(row);
            if (row.IsElite)
                row.Done = true; // 精英行：指定波次一次性刷出，不重复
            else
                row.NextSpawnFrame += row.IntervalFrames;
        }
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
                NextSpawnFrame = (long)cfg.StartMinute * 60000 / VampireLogic.LogicFrameMs, // 分钟 → 逻辑帧
                IntervalFrames = System.Math.Max(1, (int)System.Math.Round(cfg.SpawnIntervalSec * 1000 / VampireLogic.LogicFrameMs)),
                PerWave = cfg.SpawnCountPerWave,
                CfgId = enemy.Id,
                Behavior = (int)enemy.Behavior,
                Hp = (Fix.FromInt(enemy.Hp) * Fix.FromDouble(cfg.HpMul)).Int,
                MoveSpeed = Fix.FromDouble(enemy.MoveSpeed) * Fix.FromDouble(cfg.SpeedMul),
                Radius = Fix.FromDouble(enemy.Radius) * Fix.FromDouble(cfg.ViewScale), // 精英行放大系数同步碰撞半径（3-5）
                Damage = enemy.Damage,
                Exp = enemy.Exp,
                IsElite = cfg.IsElite,
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

    /// <summary>
    /// 环形刷一批：以最近玩家为圆心、视野外半径处的随机方向生成（随机方向向量代替角度，避免引入 sin/cos）。
    /// Linear 怪（3-4）：出生方向 = 指向圆心 ± 随机偏移（穿越感：斜穿而非正对圆心）。
    /// </summary>
    private void SpawnEnemies(WaveRow row)
    {
        var cx = Fix.Zero;
        var cy = Fix.Zero;
        var heroes = _logic.Heroes;
        if (heroes.Count > 0)
        {
            cx = heroes[0].X;
            cy = heroes[0].Y;
        }

        for (int i = 0; i < row.PerWave; i++)
        {
            var dx = _logic.Rng.NextFix(-1, 1);
            var dy = _logic.Rng.NextFix(-1, 1);
            var lenSq = dx * dx + dy * dy;
            if (lenSq == Fix.Zero) continue; // 极小概率，跳过本只

            var len = Fix.Sqrt(lenSq);
            var enemy = CoreMgr.ClassPool.Get<LogicEnemy>(); // 池取（Recycle 时 Reset，字段必然干净）
            enemy.Id = _logic.AllocId();
            enemy.CfgId = row.CfgId;
            enemy.Behavior = row.Behavior;
            enemy.X = cx + dx / len * _spawnRadius;
            enemy.Y = cy + dy / len * _spawnRadius;
            enemy.MoveSpeed = row.MoveSpeed;
            enemy.Radius = row.Radius;
            enemy.MaxHp = row.Hp;
            enemy.Hp = row.Hp;
            enemy.Damage = row.Damage;
            enemy.Exp = row.Exp;
            enemy.IsElite = row.IsElite ? 1 : 0; // 精英死亡掉宝箱（VsDropSystem.Spawn 分支，3-5）

            if (row.Behavior == (int)cfg.VSEnemyBehavior.Linear)
            {
                // 横穿方向：指向圆心 + 随机侧偏（±0.5 单位向量），归一化后即航向（出生方向一次性确定）
                var offX = _logic.Rng.NextFix(-5, 5) / Fix.FromInt(10); // [−0.5, 0.5)
                var offY = _logic.Rng.NextFix(-5, 5) / Fix.FromInt(10);
                var tdx = cx - enemy.X + offX;
                var tdy = cy - enemy.Y + offY;
                var tLen = Fix.Sqrt(tdx * tdx + tdy * tdy);
                if (tLen == Fix.Zero) { tdx = Fix.One; tLen = Fix.One; }
                enemy.FacingX = tdx / tLen;
                enemy.FacingY = tdy / tLen;
            }

            _logic.Enemies.Add(enemy);
        }
    }
}
