/*--------------------------------------------------------------
 * File: FrameAnimMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/25 16:40:00
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 序列帧动画管理器：
/// - 播放器池化创建/回收（CreatePlayer / ReleasePlayer），战斗视图大量增删零 GC
/// - 统一驱动（IUpdateable）：所有激活播放器在单次循环内集中推进，
///   避免每实例一个 MonoBehaviour Update 的调用开销
/// - 全局暂停（Pause / Resume）：升级选牌、战斗暂停等场景使用
/// 回收采用延迟移除：ReleasePlayer 只打标记，下一帧 flush 统一出列，
/// 因此在 onFinish 回调内回收播放器是安全的
/// </summary>
public class FrameAnimMgr : ManagerBase, IUpdateable
{
    private readonly List<FrameAnimPlayer> _activePlayers = new();
    private readonly List<FrameAnimPlayer> _freePlayers = new();

    /// <summary>当前激活播放器数量（调试用）</summary>
    public int ActiveCount => _activePlayers.Count;

    /// <summary>是否全局暂停</summary>
    public bool IsPaused { get; private set; }

    public override void OnInit()
    {
        CoreMgr.Update.RegisterUpdate(this);
    }

    /// <summary>
    /// 创建播放器（优先取池）。持有者销毁/失活视图时必须调 ReleasePlayer 归还
    /// </summary>
    /// <param name="target">换帧目标 SpriteRenderer</param>
    /// <param name="asset">动画配置</param>
    public FrameAnimPlayer CreatePlayer(SpriteRenderer target, FrameAnimAsset asset)
    {
        if (target == null || asset == null)
        {
            Log.Error("FrameAnimMgr.CreatePlayer 参数为空");
            return null;
        }

        var player = _freePlayers.Count > 0 ? PopFree() : new FrameAnimPlayer();
        player.Init(target, asset);
        _activePlayers.Add(player);
        return player;
    }

    /// <summary>
    /// 回收播放器（幂等；可在 onFinish 回调内安全调用）
    /// </summary>
    public void ReleasePlayer(FrameAnimPlayer player)
    {
        if (player == null || player.IsReleased)
        {
            return;
        }

        player.Release();
        // 不立即从 _activePlayers 移除：调用可能发生在驱动循环内，
        // 由下一帧 FlushReleased 统一出列，循环内以 IsReleased 跳过
    }

    /// <summary>全局暂停（所有播放器冻结）</summary>
    public void Pause()
    {
        IsPaused = true;
    }

    /// <summary>恢复全局播放</summary>
    public void Resume()
    {
        IsPaused = false;
    }

    /// <summary>表现层驱动，先于 Default(0) 的业务视图更新；用 deltaTime（受 timeScale 影响）</summary>
    public int Priority => 10;

    public void MyUpdate(float deltaTime, float realDeltaTime)
    {
        FlushReleased();
        if (IsPaused)
        {
            return;
        }

        for (int i = 0; i < _activePlayers.Count; i++)
        {
            var player = _activePlayers[i];
            if (!player.IsReleased)
            {
                player.Advance(deltaTime);
            }
        }
    }

    public override void OnSceneExit(int sceneTp)
    {
        // 场景退出时视图随之销毁，统一回收兜底（视图自身的 ReleasePlayer 幂等）
        ReleaseAll();
    }

    public override void OnGameRestart()
    {
        ReleaseAll();
    }

    /// <summary>
    /// 倒序交换移除已回收播放器并归还池：
    /// 被换到当前下标的元素来自已遍历的尾部，不会漏删也不会重复处理
    /// </summary>
    private void FlushReleased()
    {
        for (int i = _activePlayers.Count - 1; i >= 0; i--)
        {
            var player = _activePlayers[i];
            if (player.IsReleased)
            {
                _activePlayers[i] = _activePlayers[_activePlayers.Count - 1];
                _activePlayers.RemoveAt(_activePlayers.Count - 1);
                _freePlayers.Add(player);
            }
        }
    }

    private void ReleaseAll()
    {
        for (int i = 0; i < _activePlayers.Count; i++)
        {
            var player = _activePlayers[i];
            if (!player.IsReleased)
            {
                player.Release();
            }
            _freePlayers.Add(player);
        }
        _activePlayers.Clear();
        IsPaused = false;
    }

    private FrameAnimPlayer PopFree()
    {
        var player = _freePlayers[_freePlayers.Count - 1];
        _freePlayers.RemoveAt(_freePlayers.Count - 1);
        return player;
    }
}
