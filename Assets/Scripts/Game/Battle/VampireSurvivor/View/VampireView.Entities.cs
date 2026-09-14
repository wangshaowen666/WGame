/*--------------------------------------------------------------
 * File: VampireView.Entities.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

using cfg;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 吸血鬼幸存者（表现层 partial）：四类实体的视图对账三件套 Spawn/Refresh/Despawn。
/// 英雄/敌人/弹幕/掉落走 ViewSync 对账；异步加载期间快照实体 id 与位置（实体可能被清扫/池化复用），
/// Attach 后每逻辑帧由 Refresh 刷新、消亡时由 Despawn 归还实体池（复用，而非销毁）。
/// </summary>
public partial class VampireView
{
    // ---- 玩家视图 ----

    private HeroView SpawnHeroView(LogicHero p)
    {
        var id = p.Id;              // 快照实体 id（异步加载期间实体可能被清扫/池化复用）
        var entityId = _heroEntityId;
        var px = p.X.AsFloat;       // 快照位置（Attach 前推一帧，避免异步加载期间视图停在原点被插值渲染）
        var py = p.Y.AsFloat;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 英雄实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<HeroView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 英雄预制体缺少 HeroView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId); // 记录实体配置 Id（Despawn 归还时作池 key）
            view.PushFrame(px, py);
            _playerViews.Attach(id, view);
            // 英雄视图就绪后作为相机跟随目标（单机单人：首个英雄）
            _camera.SetHeroFollow(view.transform);
        });

        return null;
    }

    private void RefreshHeroView(LogicHero p, HeroView view)
    {
        view.PushFrame(p.X.AsFloat, p.Y.AsFloat);
        view.SetFlip(p.FacingX.AsFloat);
    }

    /// <summary>英雄视图消亡：归还实体池（复用，而非销毁）</summary>
    private void DespawnHeroView(HeroView view)
    {
        GameMgr.EntityPool.Release(view.EntityId, view.gameObject);
    }

    // ---- 敌人视图 ----

    private EnemyView SpawnEnemyView(LogicEnemy e)
    {
        var id = e.Id;             // 快照实体 id（异步加载期间实体可能被清扫/池化复用）
        var enemyCfg = GameMgr.DataTable.TbVSEnemy.Get(e.CfgId);
        var entityId = enemyCfg.EntityId; // 按敌种反查表现实体（敌种随波次变化）
        // 整体缩放 = 逻辑半径 / 敌表基准半径（3-5 精英波 ViewScale 同步放大碰撞半径，由此比例还原视觉放大；普通怪恒 1）
        var scale = enemyCfg.Radius > 0f ? e.Radius.AsFloat / enemyCfg.Radius : 1f;
        var px = e.X.AsFloat;      // 快照位置（Attach 前推一帧，避免异步加载期间视图停在原点被插值渲染）
        var py = e.Y.AsFloat;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 敌人实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<EnemyView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 敌人预制体缺少 EnemyView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId); // 记录实体配置 Id（Despawn 归还时作池 key）
            view.SetScale(scale);       // 精英放大（3-5；池化复用由 OnEnable 还原基准）
            view.PushFrame(px, py);
            _enemyViews.Attach(id, view);
        });

        return null;
    }

    private void RefreshEnemyView(LogicEnemy e, EnemyView view)
    {
        view.PushFrame(e.X.AsFloat, e.Y.AsFloat);
        view.SetFlip(e.FacingX.AsFloat);
        view.SetHp(e.Hp, e.MaxHp); // 头顶血条数据（渲染帧合并绘制；死亡被清扫前 Hp=0 即隐藏）
        if (view.OnHitFrame(e.LastHitFrame))
            SpawnHitEffect(e.X.AsFloat, e.Y.AsFloat); // 新受击：在受击敌人位置触发命中特效
        view.TickFlash();
    }

    /// <summary>敌人视图消亡：播死亡动画，Timer 计时播完（0.5s）后回池；死亡位置触发冒血特效（2-12，独立特效池不挂 EnemyView，避免死亡回池连带）</summary>
    private void DespawnEnemyView(EnemyView view)
    {
        view.PlayDie();
        var p = view.transform.localPosition; // 死亡最后位置（播放死亡动画时视图仍可对账到该 Transform）
        SpawnHitEffect(p.x, p.y);
        _dyingViews[view] = CoreMgr.Timer.StartSecondDelay(EnemyDieAnimSeconds, () =>
        {
            _dyingViews.Remove(view);
            GameMgr.EntityPool.Release(view.EntityId, view.gameObject);
        });
    }

    // ---- 弹幕视图 ----

    private ProjectileView SpawnBoltView(LogicProjectile b)
    {
        var id = b.Id;
        var entityId = _boltEntityId;
        var dirX = b.DirX.AsFloat; // 快照方向（弹体池化复用后字段会被 Reset）
        var dirY = b.DirY.AsFloat;
        var px = b.X.AsFloat;      // 快照位置（Attach 前推一帧，避免异步加载期间视图停在原点被插值渲染）
        var py = b.Y.AsFloat;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 弹幕实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<ProjectileView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 弹幕预制体缺少 ProjectileView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId); // 记录实体配置 Id（Despawn 归还时作池 key）
            view.PushFrame(px, py);
            view.SetDirection(dirX, dirY); // 创建时朝向飞行目标（直线飞行，仅此一次）
            _boltViews.Attach(id, view);
        });

        return null;
    }

    private static void RefreshBoltView(LogicProjectile b, ProjectileView view)
    {
        view.PushFrame(b.X.AsFloat, b.Y.AsFloat);
    }

    /// <summary>弹幕视图消亡：归还实体池（复用，而非销毁；池 key 用创建时记录的实体配置 Id）</summary>
    private void DespawnBoltView(ProjectileView view)
    {
        GameMgr.EntityPool.Release(view.EntityId, view.gameObject);
    }

    // ---- 掉落物视图 ----

    private DropView SpawnDropView(LogicDrop d)
    {
        var id = d.Id;             // 快照实体 id（异步加载期间实体可能被清扫/池化复用）
        var pickupCfg = GameMgr.DataTable.TbVSPickup.Get(d.PickupId);
        var entityId = pickupCfg.EntityId; // 按拾取种类反查表现实体（宝石/宝箱，3-6 表驱动）
        if (entityId <= 0)
        {
            Log.Error("[吸血鬼] 拾取物未配表现实体, PickupId:", d.PickupId);
            return null;
        }
        var px = d.X.AsFloat;      // 快照位置（Attach 前推一帧）
        var py = d.Y.AsFloat;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 掉落实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<DropView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 掉落预制体缺少 DropView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId);
            view.PushFrame(px, py);
            _dropViews.Attach(id, view);
        });

        return null;
    }

    private static void RefreshDropView(LogicDrop d, DropView view)
    {
        view.PushFrame(d.X.AsFloat, d.Y.AsFloat);
    }

    /// <summary>掉落视图消亡（被拾取，2-7）：归还实体池</summary>
    private void DespawnDropView(DropView view)
    {
        GameMgr.EntityPool.Release(view.EntityId, view.gameObject);
    }

    // ---- 区域效果视图（3-1：环绕体/光环）----

    /// <summary>区域表现实体 id：按来源武器反查（武器表 EntityId；环绕体/光环共用弹体表现，正式资源 3-1 验收后替换）</summary>
    private int ResolveFieldEntityId(LogicField f)
    {
        var weaponCfg = GameMgr.DataTable.TbVSWeapon.Get(f.WeaponId);
        return weaponCfg != null ? weaponCfg.EntityId : _boltEntityId;
    }

    private FieldView SpawnFieldView(LogicField f)
    {
        var id = f.Id;             // 快照实体 id（异步加载期间实体可能被清扫/池化复用）
        var entityId = ResolveFieldEntityId(f);
        var isOrbit = f.Type == VSFieldType.Orbit;
        var amount = f.Amount;
        var px = f.X.AsFloat;      // 快照位置（Attach 前推一帧，避免异步加载期间视图停在原点被插值渲染）
        var py = f.Y.AsFloat;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 区域实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<FieldView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 区域预制体缺少 FieldView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId);
            view.SetField(isOrbit, amount); // 环绕体 = 旋转容器；光环 = 静态圈
            view.PushFrame(px, py);
            _fieldViews.Attach(id, view);
        });

        return null;
    }

    private static void RefreshFieldView(LogicField f, FieldView view)
    {
        view.PushFrame(f.X.AsFloat, f.Y.AsFloat);
        view.PushAngle(f.Angle.Int); // 自转相位（渲染帧 ApplyField 换算旋转）
    }

    /// <summary>区域视图消亡（到期/战斗结束）：归还实体池</summary>
    private void DespawnFieldView(FieldView view)
    {
        GameMgr.EntityPool.Release(view.EntityId, view.gameObject);
    }
}
