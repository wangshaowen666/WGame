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
    /// <summary>逻辑帧间隔（毫秒）。33ms = 30 帧/秒，联机时与服务器对齐拍板</summary>
    public const int LogicFrameMs = 33;

    // ---- 玩家基础数值（后续挪配置表）----
    private static readonly Fix s_playerMoveSpeed = Fix.FromDouble(0.15); // 每帧位移，30 帧 ≈ 4.5 单位/秒
    private static readonly Fix s_playerRadius = Fix.FromDouble(0.4);
    private const long StartHp = 100;

    public bool GameOver { get; private set; }
    public int LastTickFrame { get; private set; }

    public readonly List<LogicPlayer> Players = new();

    private readonly XRng _rng;
    private int _nextId = 1; // 实体稳定自增 ID（视图对账按此 ID 增删）

    public VampireLogic(long seed)
    {
        _rng = new XRng((ulong)seed);
        // 单机单人：创建 P1 玩家（出生在地图中心）
        Players.Add(new LogicPlayer
        {
            Id = _nextId++,
            PlayerIndex = 0,
            MoveSpeed = s_playerMoveSpeed,
            Radius = s_playerRadius,
            MaxHp = StartHp,
            Hp = StartHp,
        });
    }

    /// <summary>推进一帧（固定步长，帧号从 1 连续；inputs 可为空 = 空帧）</summary>
    public void Tick(int absFrame, IList<VsInput> inputs)
    {
        if (GameOver) return;
        LastTickFrame = absFrame;

        for (int p = 0; p < Players.Count; p++)
            MovePlayer(Players[p], inputs);
    }

    /// <summary>应用该玩家的输入方向移动，并更新面朝方向</summary>
    private void MovePlayer(LogicPlayer player, IList<VsInput> inputs)
    {
        var dirX = Fix.Zero;
        var dirY = Fix.Zero;
        if (inputs != null)
            for (int i = 0; i < inputs.Count; i++)
                if (inputs[i].PlayerIndex == player.PlayerIndex)
                {
                    dirX = inputs[i].DirX;
                    dirY = inputs[i].DirY;
                    break;
                }

        if (dirX == Fix.Zero && dirY == Fix.Zero) return;

        player.X += dirX * player.MoveSpeed;
        player.Y += dirY * player.MoveSpeed;
        player.FacingX = dirX;
        player.FacingY = dirY;
    }

    /// <summary>全量状态哈希（FNV-1a 64）：复现/联机时逐帧对比验证确定性</summary>
    public ulong StateHash()
    {
        var h = 14695981039346656037UL;
        Mix(ref h, LastTickFrame);
        Mix(ref h, GameOver ? 1 : 0);
        for (int i = 0; i < Players.Count; i++)
        {
            var p = Players[i];
            Mix(ref h, p.Id);
            Mix(ref h, p.PlayerIndex);
            Mix(ref h, p.X.Raw);
            Mix(ref h, p.Y.Raw);
            Mix(ref h, p.FacingX.Raw);
            Mix(ref h, p.FacingY.Raw);
            Mix(ref h, p.Hp);
        }
        return h;
    }

    /// <summary>战斗结束清理逻辑层资源</summary>
    public void Dispose()
    {
        Players.Clear();
    }

    private static void Mix(ref ulong h, long v)
    {
        h = (h ^ (ulong)v) * 1099511628211UL;
    }
}

/// <summary>玩家实体（逻辑层）：位置/面朝/移动/血量，Tick 内由输入驱动移动</summary>
public class LogicPlayer
{
    public int Id;
    public int PlayerIndex;
    public Fix X, Y;
    public Fix FacingX, FacingY; // 当前移动方向
    public Fix MoveSpeed;
    public Fix Radius;           // 碰撞半径
    public long MaxHp;
    public long Hp;
}

/// <summary>单帧玩家输入（逻辑层输入单元）：IBattleInput 产出，帧边界采样进 Tick</summary>
public struct VsInput
{
    public int PlayerIndex;
    public Fix DirX;
    public Fix DirY;
}
