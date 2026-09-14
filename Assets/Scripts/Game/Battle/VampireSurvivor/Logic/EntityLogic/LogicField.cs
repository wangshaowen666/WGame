/*--------------------------------------------------------------
 * File: LogicField.cs
 * Author: Wsw
 * Time: 2026/09/12
 *--------------------------------------------------------------
 */

/// <summary>
/// 区域效果实体（逻辑层，3-1）：持续区域型武器的载体，类型+参数驱动（圣经环绕 = Orbit / 大蒜光环 = Aura）。
/// 跟随宿主英雄移动，按周期对范围内敌人结算（伤害+击退），到期回收；帧末集中清扫与弹幕同范式。
/// </summary>
/// <remarks>实现 IResetable 供 ClassPool 池化复用</remarks>
public class LogicField : IResetable
{
    public int Id;
    public int Type;         // 类型（VSFieldType：1=Orbit 环绕体 2=Aura 光环）
    public int OwnerId;      // 归属英雄 id
    public int WeaponId;     // 来源武器 id（表现层反查表现实体/后续升级联动）
    public int Amount;       // Orbit 环绕单元数（均分圆周；Aura 恒 1）
    public Fix X, Y;         // 当前位置（Orbit=环绕圆心随宿主；Aura=光环圆心=宿主；进 StateHash 供表现对账）
    public Fix Angle;        // Orbit 自转角度累计（表现层用它布置环绕体位置；Aura 恒 0；进 StateHash）
    public Fix Radius;       // 生效半径（Orbit=环绕半径；Aura=光环半径）
    public long Damage;      // 每次结算伤害
    public Fix Knockback;    // 击退力度
    public int TickInterval; // 结算间隔（帧，Aura 每隔 N 帧灼烧一次；Orbit 逐帧接触判定，恒 1）
    public int TickCounter;  // 结算倒计时（进 StateHash——影响下次结算时机）
    public int LifeFrames;   // 剩余存活帧（到期回收；Orbit 周期=持续时长，Aura 常驻=极大值）

    public void Reset()
    {
        Id = 0;
        Type = 0;
        OwnerId = 0;
        WeaponId = 0;
        Amount = 0;
        X = Fix.Zero;
        Y = Fix.Zero;
        Angle = Fix.Zero;
        Radius = Fix.Zero;
        Damage = 0;
        Knockback = Fix.Zero;
        TickInterval = 0;
        TickCounter = 0;
        LifeFrames = 0;
    }
}

/// <summary>区域效果类型（3-1）：与 VSWeaponBehavior.Area 配合，区分环绕体/光环两种持续区域范式</summary>
public static class VSFieldType
{
    /// <summary>环绕体（圣经）：N 单元绕宿主公转，逐帧接触判定</summary>
    public const int Orbit = 1;
    /// <summary>光环（大蒜）：以宿主为圆心的常驻圆，周期灼烧</summary>
    public const int Aura = 2;
}
