/*--------------------------------------------------------------
 * File: VampireView.Fx.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 吸血鬼幸存者（表现层 partial）：一次性表现——伤害飘字、命中特效、敌人死亡动画回池计时。
/// 均不走 ViewSync（逻辑层无对应实体）：命中/死亡时 Acquire → 播放 → 到期（或死亡动画播完）经 Timer 回池；
/// 退出战斗统一 Cancel 计时并立即归还，防回调迟到重复归还/视图泄漏。
/// </summary>
public partial class VampireView
{
    // ---- 伤害飘字（命中时 Acquire → 上浮淡出（渲染帧驱动）→ 到期回池）----

    private const float DamageTextLifeSeconds = 0.6f;
    private readonly List<DamageTextView> _activeTexts = new();
    private readonly List<float> _textLifes = new();

    private void SpawnDamageText(float x, float y, long damage)
    {
        var entityId = _damageTextEntityId;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 飘字实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<DamageTextView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 飘字预制体缺少 DamageTextView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId);
            view.Show(x, y, damage);
            _activeTexts.Add(view);
            _textLifes.Add(DamageTextLifeSeconds);
        });
    }

    /// <summary>退出战斗：飘字全部立即归还（无 Timer，无需取消）</summary>
    private void CancelActiveTexts()
    {
        for (int i = 0; i < _activeTexts.Count; i++)
            GameMgr.EntityPool.Release(_activeTexts[i].EntityId, _activeTexts[i].gameObject);
        _activeTexts.Clear();
        _textLifes.Clear();
    }

    // ---- 奖励公告飘字（3-6 宝箱开启）：复用飘字实体与生命周期列表，存活更久便于阅读 ----

    private const float RewardTextLifeSeconds = 1.4f;
    private static readonly Color RewardTextColor = new(1f, 0.82f, 0.25f, 1f); // 暖金色，区别于伤害数字

    private void SpawnRewardText(float x, float y, string content)
    {
        var entityId = _damageTextEntityId;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 奖励飘字实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<DamageTextView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 奖励飘字预制体缺少 DamageTextView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId);
            view.ShowText(x, y, content, RewardTextColor);
            _activeTexts.Add(view);
            _textLifes.Add(RewardTextLifeSeconds);
        });
    }

    // ---- 命中特效（命中时 Acquire → 播放 → Timer 计时回池——逻辑层无特效实体）----

    private const float HitEffectSeconds = 0.3f; // 特效存活时长（对齐特效资产动画时长）
    private readonly Dictionary<EffectView, CancellationTokenSource> _activeEffects = new();

    private void SpawnHitEffect(float x, float y)
    {
        var entityId = _boltHitEffectId;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 命中特效实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<EffectView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 特效预制体缺少 EffectView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId);
            view.SetPosition(x, y);
            _activeEffects[view] = CoreMgr.Timer.StartSecondDelay(HitEffectSeconds, () =>
            {
                _activeEffects.Remove(view);
                GameMgr.EntityPool.Release(view.EntityId, view.gameObject);
            });
        });
    }

    /// <summary>退出战斗：取消特效回池计时并立即归还</summary>
    private void CancelActiveEffects()
    {
        foreach (var kv in _activeEffects)
        {
            CoreMgr.Timer.Stop(kv.Value);
            GameMgr.EntityPool.Release(kv.Key.EntityId, kv.Key.gameObject);
        }
        _activeEffects.Clear();
    }

    // ---- 敌人死亡动画回池计时 ----

    private const float EnemyDieAnimSeconds = 0.5f;
    private readonly Dictionary<EnemyView, CancellationTokenSource> _dyingViews = new();

    /// <summary>退出战斗：取消剩余死亡计时并立即归还（防回调迟到重复归还/视图泄漏）</summary>
    private void CancelDyingEnemies()
    {
        foreach (var kv in _dyingViews)
        {
            CoreMgr.Timer.Stop(kv.Value);
            GameMgr.EntityPool.Release(kv.Key.EntityId, kv.Key.gameObject);
        }
        _dyingViews.Clear();
    }
}
