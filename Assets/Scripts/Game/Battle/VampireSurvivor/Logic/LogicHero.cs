/*--------------------------------------------------------------
 * File: LogicHero.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

using System.Collections.Generic;

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
