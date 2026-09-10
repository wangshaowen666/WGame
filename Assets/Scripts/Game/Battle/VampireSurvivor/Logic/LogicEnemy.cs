/*--------------------------------------------------------------
 * File: LogicEnemy.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

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
    public long MaxHp;           // 满血值（刷怪时 = 波次血量，恒定不进 StateHash；表现层头顶血条比例用）
    public long Damage;          // 碰撞伤害（敌表基础值，碰到英雄时扣除；0=纯自爆不扣血）
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
        MaxHp = 0;
        Damage = 0;
        Exp = 0;
    }
}
