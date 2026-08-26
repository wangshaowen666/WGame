/*--------------------------------------------------------------
 * File: ViewSync.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/26 10:30:00
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;

/// <summary>
/// 通用视图对账器（表现层，TD/VS 共用，提炼自 BattleTD 的字典对账模式）：
/// 每逻辑帧把模拟层实体列表与视图表对账--缺则建、多则销、存在则刷新。
/// 使用规则：
/// - 视图必须"哑"：只做表现（坐标/朝向/特效），禁止逻辑、禁止自我驱动（不走 UpdateMgr）
/// - 模拟层实体 Id 必须稳定自增不复用（BattleSim._nextId 模式），否则对账会张冠李戴
/// - spawn 返回 null 表示异步加载中（登记 pending 防止每帧重复发起），
///   加载完成后调 Attach 补挂，之后交给下一次 Sync 对账（实体已死则自动回收）
/// - Clear 为终态（战斗 Dispose 调用），之后 Attach 直接回收，防止异步回调泄漏
/// - 本类为表现层设施，可用 Dictionary/HashSet；模拟层确定性纪律不受影响
/// </summary>
public sealed class ViewSync<TSim, TView> where TView : class
{
    private readonly Func<TSim, int> _getId;
    private readonly Func<int, TView> _spawn;
    private readonly Action<TSim, TView> _refresh;
    private readonly Action<TView> _despawn;

    private readonly Dictionary<int, TView> _views = new();
    private readonly HashSet<int> _pending = new(); // 异步加载中的 id
    private readonly HashSet<int> _alive = new();   // 本帧存活 id（复用容器，对账 O(n)）
    private readonly List<int> _scratch = new();    // 待删除 id 收集（避免遍历中修改字典）
    private bool _cleared;

    public ViewSync(Func<TSim, int> getId, Func<int, TView> spawn,
        Action<TSim, TView> refresh, Action<TView> despawn)
    {
        _getId = getId;
        _spawn = spawn;
        _refresh = refresh;
        _despawn = despawn;
    }

    /// <summary>当前存活视图数</summary>
    public int Count => _views.Count;

    /// <summary>按实体 Id 查视图（如受击闪白等表现层单点访问）</summary>
    public bool TryGet(int id, out TView view) => _views.TryGetValue(id, out view);

    /// <summary>
    /// 对账入口：每逻辑帧 Sim.Tick 后调用一次（容器复用，零 GC）
    /// </summary>
    public void Sync(List<TSim> sims)
    {
        if (_cleared) return;

        // 1. 刷存活：缺视图则建（null = 异步加载中），存在则刷新
        _alive.Clear();
        for (int i = 0; i < sims.Count; i++)
        {
            var sim = sims[i];
            int id = _getId(sim);
            _alive.Add(id);

            if (_views.TryGetValue(id, out var view))
            {
                _refresh(sim, view);
                continue;
            }

            if (_pending.Contains(id)) continue; // 加载中，等 Attach

            view = _spawn(id);
            if (view == null)
            {
                _pending.Add(id);
                continue;
            }
            _views[id] = view;
            _refresh(sim, view);
        }

        // 2. 销毁模拟层已不存在的视图（先收集再删，避免遍历中修改字典）
        if (_views.Count > 0)
        {
            _scratch.Clear();
            foreach (var kv in _views)
                if (!_alive.Contains(kv.Key))
                    _scratch.Add(kv.Key);
            for (int i = 0; i < _scratch.Count; i++)
                if (_views.Remove(_scratch[i], out var view))
                    _despawn(view);
        }

        // 3. 解除已死亡实体的加载登记（其视图晚到后由上面的销毁对账自动回收）
        if (_pending.Count > 0)
        {
            _scratch.Clear();
            foreach (int id in _pending)
                if (!_alive.Contains(id))
                    _scratch.Add(id);
            for (int i = 0; i < _scratch.Count; i++)
                _pending.Remove(_scratch[i]);
        }
    }

    /// <summary>
    /// 异步视图加载完成补挂（与 ViewPool.Acquire 回调配合）：
    /// 实体仍存活则入表等下一次 Sync 刷新；战斗已结束或重复补挂则立即回收
    /// </summary>
    public void Attach(int id, TView view)
    {
        if (view == null) return;

        if (_cleared || _views.ContainsKey(id))
        {
            _despawn(view);
            return;
        }

        _pending.Remove(id);
        _views[id] = view;
    }

    /// <summary>清空全部视图与加载登记（战斗 Dispose 时调用；之后本实例不可再用）</summary>
    public void Clear()
    {
        if (_cleared) return;
        _cleared = true;

        foreach (var kv in _views)
            _despawn(kv.Value);
        _views.Clear();
        _pending.Clear();
        _alive.Clear();
        _scratch.Clear();
    }
}
