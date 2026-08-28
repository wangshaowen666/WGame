/*--------------------------------------------------------------
 * File: BattleLogic.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/18 16:30:00
 *--------------------------------------------------------------
 */

using System.Collections.Generic;

/// <summary>
/// 确定性塔防逻辑层（纯 C#，无 UnityEngine）：
/// - 以服务器绝对帧号锚定：Tick(absFrame, inputs)，同一帧号 + 同一操作序列 => 任意客户端状态完全一致
/// - 构造传入随机种子（由 StartGamePush 统一下发，双端一致）
/// - 禁止：float/double、UnityEngine.Random、Time.*、Dictionary 遍历（本类只用 List/按索引访问）
/// - 操作类型：1=放塔(param1=格子x, param2=格子y) 2=升级塔(同参数，需塔主人)
/// </summary>
public class TdLogic
{
    // ---- 地图与规则常量（阶段 6 起由 StartGame 消息/配置表下发） ----
    // 竖屏布局：窄×高（配合俯视相机，手机竖屏友好）
    public const int MapW = 8;
    public const int MapH = 16;
    public const int SpawnEveryFrames = 60;    // 每 60 帧（3 秒）一波
    public const int MaxAliveEnemies = 60;     // 场上敌人上限（无塔时防状态爆炸）

    public const long StartGold = 200;         // 起始金币（4 座塔起步，测试期让塔能成型）
    public const int TowerCost = 50;
    public const int UpgradeCost = 30;
    public const int MaxTowerLevel = 3;
    public const int KillReward = 12;
    public const int StartBaseHp = 10;

    public const long EnemyBaseHp = 40;        // 下调起始血量，1 级塔能打死前几波
    public const long EnemyHpGrowPerWave = 20;
    public const int TowerIntervalFrames = 5; // 攻击间隔 1 秒（原 2 秒太慢）
    public const int TowerBaseDamage = 40;
    public const int TowerDamagePerLevel = 20; // 1/2/3 级: 40/60/80 伤害

    /// <summary>敌人（有序列表，遍历与目标选择顺序确定）</summary>
    public class LogicEnemy
    {
        public int Id;
        public long Hp;
        public Fix X, Y;
        public int WpIndex; // 当前前往的路径点下标
    }

    /// <summary>塔</summary>
    public class LogicTower
    {
        public int Id;
        public int OwnerPlayerId;
        public int CellX, CellY;
        public int Level = 1;
        public int Cooldown; // 距下次可攻击的剩余帧数
    }

    /// <summary>玩家（按初始化顺序排列，遍历顺序确定）</summary>
    public class LogicPlayer
    {
        public int PlayerId;
        public long Gold = StartGold;
    }

    // ---- 业务数值（定点常量直接写业务值，FromDouble 内部转 raw，见 FixMath） ----
    private static readonly Fix s_enemySpeed = Fix.FromDouble(0.07);   // 0.07 格/帧 = 1.4 格/秒
    private static readonly Fix s_towerRange = Fix.FromDouble(3.0);    // 塔射程 3 格

    // 路径点（格子中心坐标），敌人从 Wp[0] 走到 Wp[last]，竖向蛇形布局
    private static readonly (int X, int Y)[] s_waypoints =
    {
        (0, 0), (0, 6), (3, 6), (3, 11), (6, 11), (6, 15),
    };

    public readonly List<LogicEnemy> Enemies = new();
    public readonly List<LogicTower> Towers = new();
    public readonly List<LogicPlayer> Players = new();

    public int BaseHp { get; private set; } = StartBaseHp;
    public bool GameOver { get; private set; }
    public int LastTickFrame { get; private set; }

    /// <summary>最近一次被拒绝的操作原因（成功/无操作时为空，仅诊断用，不影响确定性）</summary>
    public string LastReject { get; private set; } = "";

    private readonly XRng _rng;

    /// <summary>
    /// 构造。seed/初始玩家列表来自 StartGamePush（双端一致）；
    /// playerIds 为空时（旧测试入口）不预登记，保持"首次操作进场"的旧行为
    /// </summary>
    public TdLogic(long seed = 12345, int[]? playerIds = null)
    {
        _rng = new XRng((ulong)seed);

        // 开局登记参战玩家（金币=StartGold；阶段 7 可改为从养成数据带初始金币）
        if (playerIds != null)
            foreach (var pid in playerIds)
                Players.Add(new LogicPlayer { PlayerId = pid });
    }
    private int _nextId = 1;

    /// <summary>
    /// 推进一帧。absFrame 必须从 1 开始连续调用（空帧也要 tick，可用空 inputs 快进）
    /// </summary>
    public void Tick(int absFrame, IList<NetMsg.PlayerInput> inputs)
    {
        if (GameOver) return;
        LastTickFrame = absFrame;

        // 1. 应用操作（按帧内顺序，双端一致）
        if (inputs != null)
            for (int i = 0; i < inputs.Count; i++)
                ApplyInput(inputs[i]);

        // 2. 刷怪（按绝对帧号调度，波次/数量/血量只由帧号决定）
        if (absFrame % SpawnEveryFrames == 0)
        {
            var wave = absFrame / SpawnEveryFrames;
            var count = 1 + wave / 2;
            if (count > 5) count = 5;
            var hp = EnemyBaseHp + wave * EnemyHpGrowPerWave;
            for (int i = 0; i < count && Enemies.Count < MaxAliveEnemies; i++)
                SpawnEnemy(hp);
        }

        // 3. 塔攻击（按塔列表顺序）
        for (int t = 0; t < Towers.Count; t++)
            TowerFire(Towers[t]);

        // 4. 敌人移动（按敌人列表顺序）
        for (int e = Enemies.Count - 1; e >= 0; e--)
            MoveEnemy(Enemies[e]);

        // 5. 清理死亡/漏怪敌人（RemoveAll 保持存活者相对顺序，结果确定）
        Enemies.RemoveAll(en => en.Hp <= 0);

        // 6. 失败判定
        if (BaseHp <= 0)
        {
            BaseHp = 0;
            GameOver = true;
        }
    }

    /// <summary>
    /// 应用一条玩家操作
    /// </summary>
    private void ApplyInput(NetMsg.PlayerInput input)
    {
        switch (input.OpType)
        {
            case 1: // 放塔
                PlaceTower(input.PlayerId, input.Param1, input.Param2);
                break;
            case 2: // 升级塔
                UpgradeTower(input.PlayerId, input.Param1, input.Param2);
                break;
        }
    }

    private void PlaceTower(int playerId, int cellX, int cellY)
    {
        if (cellX < 0 || cellX >= MapW || cellY < 0 || cellY >= MapH)
        {
            LastReject = $"放塔({cellX},{cellY}) 越界";
            return;
        }

        // 格子已被占用（先校验后登记玩家，避免非法操作把玩家加进列表）
        for (int i = 0; i < Towers.Count; i++)
            if (Towers[i].CellX == cellX && Towers[i].CellY == cellY)
            {
                LastReject = $"放塔({cellX},{cellY}) 格子已被占用";
                return;
            }

        var player = GetOrAddPlayer(playerId);
        if (player.Gold < TowerCost)
        {
            LastReject = $"放塔({cellX},{cellY}) 金币不足(需{TowerCost})";
            return;
        }

        LastReject = "";
        player.Gold -= TowerCost;
        Towers.Add(new LogicTower
        {
            Id = _nextId++,
            OwnerPlayerId = playerId,
            CellX = cellX,
            CellY = cellY,
        });
    }

    private void UpgradeTower(int playerId, int cellX, int cellY)
    {
        for (int i = 0; i < Towers.Count; i++)
        {
            var tower = Towers[i];
            if (tower.CellX != cellX || tower.CellY != cellY) continue;

            // 只允许塔主人升级
            if (tower.OwnerPlayerId != playerId)
            {
                LastReject = $"升级({cellX},{cellY}) 只能升级自己的塔";
                return;
            }
            if (tower.Level >= MaxTowerLevel)
            {
                LastReject = $"升级({cellX},{cellY}) 已满级";
                return;
            }

            var player = GetOrAddPlayer(playerId);
            if (player.Gold < UpgradeCost)
            {
                LastReject = $"升级({cellX},{cellY}) 金币不足(需{UpgradeCost})";
                return;
            }

            LastReject = "";
            player.Gold -= UpgradeCost;
            tower.Level++;
            return;
        }

        LastReject = $"升级({cellX},{cellY}) 该位置没有塔";
    }

    /// <summary>按 id 查玩家，不存在则进场（首次操作的顺序决定 Players 列表顺序，双端一致）</summary>
    private LogicPlayer GetOrAddPlayer(int playerId)
    {
        for (int i = 0; i < Players.Count; i++)
            if (Players[i].PlayerId == playerId)
                return Players[i];

        var p = new LogicPlayer { PlayerId = playerId };
        Players.Add(p);
        return p;
    }

    private void SpawnEnemy(long hp)
    {
        var (wx, wy) = s_waypoints[0];
        Enemies.Add(new LogicEnemy
        {
            Id = _nextId++,
            Hp = hp,
            X = Fix.FromInt(wx) + Fix.Half,
            Y = Fix.FromInt(wy) + Fix.Half + _rng.NextFix(-0.03, 0.03), // 出生散布 ±1966 raw ≈ ±0.03 格
            WpIndex = 1,
        });
    }

    /// <summary>
    /// 塔攻击：冷却递减，就绪后打"距离最近的存活敌人"（并列取列表靠前即更早刷出的，结果确定）
    /// </summary>
    private void TowerFire(LogicTower tower)
    {
        if (tower.Cooldown > 0)
        {
            tower.Cooldown--;
            return;
        }

        var tx = Fix.FromInt(tower.CellX) + Fix.Half;
        var ty = Fix.FromInt(tower.CellY) + Fix.Half;
        var range = s_towerRange + Fix.FromInt(tower.Level - 1) / Fix.FromInt(4);
        var rangeSq = range * range;

        LogicEnemy target = null;
        var bestSq = Fix.Zero;
        for (int i = 0; i < Enemies.Count; i++)
        {
            var en = Enemies[i];
            if (en.Hp <= 0) continue;

            var dx = en.X - tx;
            var dy = en.Y - ty;
            var dSq = dx * dx + dy * dy;
            if (dSq > rangeSq) continue;

            if (target == null || dSq < bestSq)
            {
                target = en;
                bestSq = dSq;
            }
        }

        if (target == null) return; // 无目标不进入冷却，下一帧继续找

        tower.Cooldown = TowerIntervalFrames;
        target.Hp -= TowerBaseDamage + (tower.Level - 1) * TowerDamagePerLevel;
        if (target.Hp <= 0)
        {
            // 击杀奖励给塔主人
            for (int i = 0; i < Players.Count; i++)
                if (Players[i].PlayerId == tower.OwnerPlayerId)
                {
                    Players[i].Gold += KillReward;
                    break;
                }
        }
    }

    /// <summary>
    /// 敌人向当前路径点匀速移动；到达终点扣基地血
    /// </summary>
    private void MoveEnemy(LogicEnemy en)
    {
        if (en.WpIndex >= s_waypoints.Length)
        {
            // 已在终点：漏怪
            BaseHp--;
            en.Hp = 0; // 标记移除
            return;
        }

        var (wx, wy) = s_waypoints[en.WpIndex];
        var tx = Fix.FromInt(wx) + Fix.Half;
        var ty = Fix.FromInt(wy) + Fix.Half;

        var dx = tx - en.X;
        var dy = ty - en.Y;
        var dist = Fix.Sqrt(dx * dx + dy * dy);

        if (dist <= s_enemySpeed)
        {
            // 本帧到达路径点
            en.X = tx;
            en.Y = ty;
            en.WpIndex++;
        }
        else
        {
            // 沿单位向量移动一步
            en.X += dx / dist * s_enemySpeed;
            en.Y += dy / dist * s_enemySpeed;
        }
    }

    /// <summary>
    /// 全量状态哈希（FNV-1a 64）：双端逐帧对比此值即可验证确定性
    /// </summary>
    public ulong StateHash()
    {
        var h = 14695981039346656037UL;
        Mix(ref h, LastTickFrame);
        Mix(ref h, BaseHp);
        Mix(ref h, _nextId);

        for (int i = 0; i < Players.Count; i++)
        {
            Mix(ref h, Players[i].PlayerId);
            Mix(ref h, Players[i].Gold);
        }

        for (int i = 0; i < Enemies.Count; i++)
        {
            var en = Enemies[i];
            Mix(ref h, en.Id);
            Mix(ref h, en.Hp);
            Mix(ref h, en.X.Raw);
            Mix(ref h, en.Y.Raw);
            Mix(ref h, en.WpIndex);
        }

        for (int i = 0; i < Towers.Count; i++)
        {
            var tw = Towers[i];
            Mix(ref h, tw.Id);
            Mix(ref h, tw.OwnerPlayerId);
            Mix(ref h, tw.CellX);
            Mix(ref h, tw.CellY);
            Mix(ref h, tw.Level);
            Mix(ref h, tw.Cooldown);
        }
        return h;
    }

    private static void Mix(ref ulong h, long v)
    {
        h = (h ^ (ulong)v) * 1099511628211UL;
    }
}
