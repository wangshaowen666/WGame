/*--------------------------------------------------------------
 * File: LogicDrop.cs
 * Author: Wsw
 *--------------------------------------------------------------
 */

/// <summary>
/// 掉落物实体（逻辑层，2-6：死亡掉落经验宝石）：静止躺在死亡位置，磁吸/拾取阶段 2-7 实现。
/// 3-5 扩展：精英怪掉宝箱（PickupId 区分拾取种类，拾取分支见 VsDropSystem）。
/// </summary>
/// <remarks>实现 IResetable 供 ClassPool 池化复用</remarks>
public class LogicDrop : IResetable
{
    public int Id;
    public Fix X, Y;
    public long Value;   // 经验值（来自敌人表 Exp；宝箱=0）
    public int PickupId; // 拾取种类（#VSPickup 表 id：宝石/宝箱；表现层反查表现实体）

    public void Reset()
    {
        Id = 0;
        X = Fix.Zero;
        Y = Fix.Zero;
        Value = 0;
        PickupId = 0;
    }
}
