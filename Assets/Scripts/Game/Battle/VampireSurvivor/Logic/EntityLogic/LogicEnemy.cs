/*--------------------------------------------------------------
 * File: LogicEnemy.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

/// <summary>敌人实体（逻辑层）：行为按 CfgId 查敌表 Behavior 分发（追击/直线横穿），接触玩家自爆并扣碰撞伤害。</summary>
/// <remarks>实现 IResetable 供 ClassPool 池化复用（Recycle 时 Reset 全清，避免脏数据）</remarks>
public class LogicEnemy : IResetable
{
    public int Id;
    public int CfgId;            // 敌种配置 id（#VSEnemy，逻辑身份；表现层据此反查表现实体/掉落）
    public int Behavior;         // 行为类型（VSEnemyBehavior 值，刷怪时自敌表缓存；进 StateHash）
    public int LastHitFrame;     // 最后被弹幕命中的逻辑帧（受击感知，表现向：不进 StateHash，播闪白用）
    public Fix X, Y;
    public Fix FacingX, FacingY; // 追击/横穿方向（表现层翻转用）
    public Fix MoveSpeed;
    public Fix Radius;           // 碰撞半径
    public Fix KnockbackX, KnockbackY; // 击退冲量（每帧位移并衰减，进 StateHash——影响位置演算）
    public long Hp;
    public long MaxHp;           // 满血值（刷怪时 = 波次血量，恒定不进 StateHash；表现层头顶血条比例用）
    public long Damage;          // 碰撞伤害（敌表基础值，碰到英雄时扣除；0=纯自爆不扣血）
    public long Exp;             // 击杀掉落经验（敌表基础值，波次系数不作用于经验）
    public int IsElite;          // 精英标记（3-5：波次表精英行刷出，死亡掉宝箱；0/1 进 StateHash）

    public void Reset()
    {
        Id = 0;
        CfgId = 0;
        Behavior = 0;
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
        IsElite = 0;
    }
}
