/*--------------------------------------------------------------
 * File: LogicProjectile.cs
 * Author: Wsw
 *--------------------------------------------------------------
 */

/// <summary>
/// 弹幕实体（逻辑层）：直线飞行、圆形点命中判定（距敌圆心 ≤ 敌半径，MVP 暴力 O(n×m)，阶段 7-1 空间网格）。
/// 归属英雄发射，命中扣血并消耗穿透，穿透耗尽或超时回收。
/// </summary>
/// <remarks>实现 IResetable 供 ClassPool 池化复用（高频实体，阶段 2 验收同屏 100 弹）</remarks>
public class LogicProjectile : IResetable
{
    public int Id;
    public Fix X, Y;
    public Fix DirX, DirY;   // 单位方向
    public Fix MoveSpeed;    // 每帧位移（表配单位/秒，开火时换算）
    public long Damage;
    public int Pierce;       // 剩余可命中数（命中 1 敌 -1，0 回收）
    public int LifeFrames;   // 剩余存活帧（超时回收）
    public int OwnerId;      // 归属英雄 id（伤害统计/双人用）
    public int LastHitEnemyId; // 同弹对同敌免重复判定（0=无）
    public Fix Knockback;    // 击退力度（命中时沿飞行方向推敌人，来自武器表；进 StateHash）

    public void Reset()
    {
        Id = 0;
        X = Fix.Zero;
        Y = Fix.Zero;
        DirX = Fix.Zero;
        DirY = Fix.Zero;
        MoveSpeed = Fix.Zero;
        Damage = 0;
        Pierce = 0;
        LifeFrames = 0;
        OwnerId = 0;
        LastHitEnemyId = 0;
        Knockback = Fix.Zero;
    }
}
