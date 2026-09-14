/*--------------------------------------------------------------
 * File: VsEnemySystem.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

/// <summary>
/// 敌人行为系统（纯 C# 确定性）：行为按敌表 Behavior 分发（3-4）——
/// Chase/ChaseSuicide：追击最近存活玩家（叠加击退位移与衰减）；
/// Linear：直线横穿（出生方向匀速直行，不索敌，越出场地边界回收）。
/// Tick 内先全体移动再统一接触判定，两步顺序由 VampireLogic.Tick 编排（顺序即确定性）。
/// </summary>
public sealed class VsEnemySystem
{
    private static readonly Fix s_knockbackDecay = Fix.FromDouble(0.8); // 每逻辑帧击退速度衰减系数（20Hz 语义）

    private readonly VampireLogic _logic;
    private readonly Fix _boundX; // 横穿回收边界半宽（场地半宽 + 余量）
    private readonly Fix _boundY; // 横穿回收边界半高

    /// <summary>横穿怪越界余量（单位，出视野足够远再回收，避免可见消失）</summary>
    private const float DespawnMargin = 3f;

    public VsEnemySystem(VampireLogic logic)
    {
        _logic = logic;
        var stage = GameMgr.DataTable.TbVSStage.Get(VampireLogic.StageId);
        _boundX = Fix.FromDouble(stage.FieldWidth * 0.5 + DespawnMargin);
        _boundY = Fix.FromDouble(stage.FieldHeight * 0.5 + DespawnMargin);
    }

    /// <summary>敌人移动（Tick 步骤 5）：按行为分发</summary>
    public void TickMove()
    {
        var enemies = _logic.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            var en = enemies[i];
            if (en.Behavior == (int)cfg.VSEnemyBehavior.Linear)
                MoveLinear(en);
            else
                MoveChase(en);
        }
    }

    /// <summary>接触判定（Tick 步骤 6）：接触玩家则敌人自毁（Hp=0 交由帧末集中清扫）并按敌表 Damage 扣血（0=纯自爆不扣血；Hp<=0 由帧末 GameOver 判定兜底）</summary>
    public void TickSuicide()
    {
        var enemies = _logic.Enemies;
        for (int i = 0; i < enemies.Count; i++)
            EnemySuicide(enemies[i]);
    }

    /// <summary>追击：向最近存活玩家匀速移动（叠加击退位移与衰减），并更新面朝方向</summary>
    private void MoveChase(LogicEnemy en)
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

    /// <summary>直线横穿（3-4 蝙蝠式）：沿出生方向匀速直行（不索敌；FacingX/Y 即飞行方向），越出场地边界（含余量）回收</summary>
    private void MoveLinear(LogicEnemy en)
    {
        // 击退位移叠加（可被弹幕推离航线，但不改变航向）
        en.X += en.KnockbackX;
        en.Y += en.KnockbackY;
        en.KnockbackX = en.KnockbackX * s_knockbackDecay;
        en.KnockbackY = en.KnockbackY * s_knockbackDecay;

        en.X += en.FacingX * en.MoveSpeed;
        en.Y += en.FacingY * en.MoveSpeed;

        // 越界回收：Hp=0 交由帧末集中清扫（直接 Recycle 会破坏遍历序；横穿怪自然离场不计击杀不掉落）
        if (en.X < -_boundX || en.X > _boundX || en.Y < -_boundY || en.Y > _boundY)
            en.Hp = 0;
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

    /// <summary>敌人状态进哈希（Behavior/IsElite 影响行为与掉落分支；不含 MaxHp/LastHitFrame/MoveSpeed/Radius）</summary>
    public void HashState(ref ulong h)
    {
        var enemies = _logic.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            VsHash.Mix(ref h, e.Id);
            VsHash.Mix(ref h, e.CfgId);
            VsHash.Mix(ref h, e.Behavior);
            VsHash.Mix(ref h, e.X.Raw);
            VsHash.Mix(ref h, e.Y.Raw);
            VsHash.Mix(ref h, e.FacingX.Raw);
            VsHash.Mix(ref h, e.FacingY.Raw);
            VsHash.Mix(ref h, e.KnockbackX.Raw);
            VsHash.Mix(ref h, e.KnockbackY.Raw);
            VsHash.Mix(ref h, e.Hp);
            VsHash.Mix(ref h, e.Damage);
            VsHash.Mix(ref h, e.Exp);
            VsHash.Mix(ref h, e.IsElite); // 3-5 追加（尾部追加不破坏既有哈希序）
        }
    }
}
