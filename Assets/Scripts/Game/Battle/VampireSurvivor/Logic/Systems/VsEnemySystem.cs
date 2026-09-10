/*--------------------------------------------------------------
 * File: VsEnemySystem.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

/// <summary>
/// 敌人行为系统（纯 C# 确定性）：追击最近存活玩家（叠加击退位移与衰减）、接触玩家自爆。
/// Tick 内先全体移动再统一自爆判定，两步顺序由 VampireLogic.Tick 编排（顺序即确定性）。
/// </summary>
public sealed class VsEnemySystem
{
    private static readonly Fix s_knockbackDecay = Fix.FromDouble(0.8); // 每逻辑帧击退速度衰减系数（20Hz 语义）

    private readonly VampireLogic _logic;

    public VsEnemySystem(VampireLogic logic)
    {
        _logic = logic;
    }

    /// <summary>敌人移动（Tick 步骤 5）：向最近存活玩家匀速移动</summary>
    public void TickMove()
    {
        var enemies = _logic.Enemies;
        for (int i = 0; i < enemies.Count; i++)
            MoveEnemy(enemies[i]);
    }

    /// <summary>自爆判定（Tick 步骤 6）：接触玩家则敌人自毁（Hp=0 交由帧末集中清扫）并按敌表 Damage 扣血（0=纯自爆不扣血；Hp<=0 由帧末 GameOver 判定兜底）</summary>
    public void TickSuicide()
    {
        var enemies = _logic.Enemies;
        for (int i = 0; i < enemies.Count; i++)
            EnemySuicide(enemies[i]);
    }

    /// <summary>敌人向最近存活玩家匀速移动（叠加击退位移与衰减），并更新面朝方向</summary>
    private void MoveEnemy(LogicEnemy en)
    {
        // 击退位移（冲量随帧衰减）：与追击位移叠加，被弹幕命中时被推离
        en.X += en.KnockbackX;
        en.Y += en.KnockbackY;
        en.KnockbackX = en.KnockbackX * s_knockbackDecay;
        en.KnockbackY = en.KnockbackY * s_knockbackDecay;

        var heroes = _logic.Heroes;
        LogicHero target = null;
        var bestSq = Fix.Zero;
        for (int h = 0; h < heroes.Count; h++)
        {
            var hero = heroes[h];
            if (hero.Hp <= 0) continue;
            var dx = hero.X - en.X;
            var dy = hero.Y - en.Y;
            var dSq = dx * dx + dy * dy;
            if (target == null || dSq < bestSq)
            {
                target = hero;
                bestSq = dSq;
            }
        }
        if (target == null) return;

        var tdx = target.X - en.X;
        var tdy = target.Y - en.Y;
        var dist = Fix.Sqrt(tdx * tdx + tdy * tdy);
        if (dist <= en.MoveSpeed)
        {
            en.X = target.X;
            en.Y = target.Y;
        }
        else
        {
            en.X += tdx / dist * en.MoveSpeed;
            en.Y += tdy / dist * en.MoveSpeed;
        }
        en.FacingX = tdx;
        en.FacingY = tdy;
    }

    private void EnemySuicide(LogicEnemy en)
    {
        var heroes = _logic.Heroes;
        for (int h = 0; h < heroes.Count; h++)
        {
            var hero = heroes[h];
            if (hero.Hp <= 0) continue;
            var r = en.Radius + hero.Radius;
            var dx = hero.X - en.X;
            var dy = hero.Y - en.Y;
            if (dx * dx + dy * dy <= r * r)
            {
                en.Hp = 0;
                if (en.Damage > 0)
                    hero.Hp -= en.Damage;
                return;
            }
        }
    }

    /// <summary>敌人状态进哈希（字段与顺序与拆分前一致：不含 MaxHp/LastHitFrame/MoveSpeed/Radius）</summary>
    public void HashState(ref ulong h)
    {
        var enemies = _logic.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            VsHash.Mix(ref h, e.Id);
            VsHash.Mix(ref h, e.CfgId);
            VsHash.Mix(ref h, e.X.Raw);
            VsHash.Mix(ref h, e.Y.Raw);
            VsHash.Mix(ref h, e.FacingX.Raw);
            VsHash.Mix(ref h, e.FacingY.Raw);
            VsHash.Mix(ref h, e.KnockbackX.Raw);
            VsHash.Mix(ref h, e.KnockbackY.Raw);
            VsHash.Mix(ref h, e.Hp);
            VsHash.Mix(ref h, e.Damage);
            VsHash.Mix(ref h, e.Exp);
        }
    }
}
