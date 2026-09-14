/*--------------------------------------------------------------
 * File: LogicProjectile.cs
 * Author: Wsw
 *--------------------------------------------------------------
 */

/// <summary>
/// 弹幕实体（逻辑层）：弹道变体（3-1）——
/// Gravity=0 直线飞行；Gravity>0 抛物线（斧头）：初速含竖直分量，每帧重力积分 vy -= g*dt，命中与超时回收同直线弹。
/// 圆形点命中判定（距敌圆心 ≤ 敌半径，MVP 暴力 O(n×m)，阶段 7-1 空间网格）。
/// 归属英雄发射，命中扣血并消耗穿透，穿透耗尽或超时回收。
/// </summary>
/// <remarks>实现 IResetable 供 ClassPool 池化复用（高频实体，阶段 2 验收同屏 100 弹）</remarks>
public class LogicProjectile : IResetable
{
    public int Id;
    public Fix X, Y;
    public Fix DirX, DirY;   // 单位方向（XY=水平面；抛物线时 DirY 另承担竖直分量，见 Gravity）
    public Fix MoveSpeed;    // 每帧水平位移（表配单位/秒，开火时换算）
    public long Damage;
    public int Pierce;       // 剩余可命中数（命中 1 敌 -1，0 回收）
    public int LifeFrames;   // 剩余存活帧（超时回收）
    public int OwnerId;      // 归属英雄 id（伤害统计/双人用）
    public int LastHitEnemyId; // 同弹对同敌免重复判定（0=无）
    public Fix Knockback;    // 击退力度（命中时沿飞行方向推敌人，来自武器表；进 StateHash）
    public Fix Gravity;      // 竖直重力加速度（0=直线弹道；>0 抛物线：Y 为竖直高度，Z 落地距离，3-1）

    // 抛物线专用运行时状态（直线弹恒 0；进 StateHash——影响位置演算）
    public Fix VertVel;      // 竖直速度（单位/帧）
    public Fix HorizX, HorizY; // 水平单位方向（抛物线时 DirX/DirY 存"水平+竖直"合成速度，本字段存纯水平朝向供翻转/落地推进）
    public Fix GroundX, GroundY; // 落地点水平位置（抛物线水平推进基准）
    public Fix Height;       // 当前竖直高度（>0 在空中；≤0 落地回收）
    public int HitWhenLanded; // 落地后仍可命中数（穿透在空中不消耗：斧头下落落地才判定）

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
        Gravity = Fix.Zero;
        VertVel = Fix.Zero;
        HorizX = Fix.Zero;
        HorizY = Fix.Zero;
        GroundX = Fix.Zero;
        GroundY = Fix.Zero;
        Height = Fix.Zero;
        HitWhenLanded = 0;
    }
}
