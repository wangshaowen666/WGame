/*--------------------------------------------------------------
 * File: VsDropSystem.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

/// <summary>
/// 掉落系统（2-7，纯 C# 确定性）：敌人被弹幕击杀时生成经验宝石；
/// 磁吸与拾取以最近存活英雄为参照——进入拾取半径（英雄碰撞半径）即拾取入账经验并回池；
/// 进入磁吸半径则向英雄移动（吸引速度＞玩家移速，必然追上）；两半径外静止躺尸待捡。
/// 经验入账走升级系统（SysLevelUp.GainXp）。
/// </summary>
public sealed class VsDropSystem
{
    private static readonly Fix s_dropAttractSpeed = Fix.FromDouble(0.6); // 掉落物磁吸位移速度（单位/逻辑帧）

    private readonly VampireLogic _logic;

    public VsDropSystem(VampireLogic logic)
    {
        _logic = logic;
    }

    /// <summary>死亡掉落：敌人被弹幕击杀时生成经验宝石（IResetable 池化；价值=敌种 Exp，敌种间自然分档，视觉分档 7-3 治理时再配）</summary>
    public void Spawn(LogicEnemy en)
    {
        var drop = CoreMgr.ClassPool.Get<LogicDrop>();
        drop.Id = _logic.AllocId();
        drop.X = en.X;
        drop.Y = en.Y;
        drop.Value = en.Exp;
        _logic.Drops.Add(drop);
    }

    /// <summary>掉落物磁吸与拾取（Tick 步骤 8）：经验入账并触发升级结算</summary>
    public void Tick()
    {
        var drops = _logic.Drops;
        var heroes = _logic.Heroes;
        for (int i = drops.Count - 1; i >= 0; i--) // 倒序：拾取即回池移除
        {
            var drop = drops[i];

            // 最近存活英雄（联机双人时磁吸/拾取归属最近者）
            LogicHero target = null;
            var bestSq = Fix.Zero;
            for (int h = 0; h < heroes.Count; h++)
            {
                var hero = heroes[h];
                if (hero.Hp <= 0) continue;
                var dx = hero.X - drop.X;
                var dy = hero.Y - drop.Y;
                var dSq = dx * dx + dy * dy;
                if (target == null || dSq < bestSq)
                {
                    target = hero;
                    bestSq = dSq;
                }
            }
            if (target == null) return; // 无存活英雄：掉落全部静止（帧末 GameOver 判定兜底）

            var pickupR = target.Radius;
            if (bestSq <= pickupR * pickupR)
            {
                _logic.SysLevelUp.GainXp(target, drop.Value);
                CoreMgr.ClassPool.Recycle(drop);
                drops.RemoveAt(i);
                continue;
            }

            var magnetR = target.MagnetRadius;
            if (bestSq > magnetR * magnetR)
                continue;

            // 磁吸：朝英雄移动，步长不超过剩余距离（防越过目标抖动；此处 dist＞拾取半径＞0，除法安全）
            var dist = Fix.Sqrt(bestSq);
            var step = dist < s_dropAttractSpeed ? dist : s_dropAttractSpeed;
            drop.X += (target.X - drop.X) / dist * step;
            drop.Y += (target.Y - drop.Y) / dist * step;
        }
    }

    /// <summary>掉落状态进哈希（字段与顺序与拆分前一致）</summary>
    public void HashState(ref ulong h)
    {
        var drops = _logic.Drops;
        for (int i = 0; i < drops.Count; i++)
        {
            var d = drops[i];
            VsHash.Mix(ref h, d.Id);
            VsHash.Mix(ref h, d.X.Raw);
            VsHash.Mix(ref h, d.Y.Raw);
            VsHash.Mix(ref h, d.Value);
        }
    }
}
