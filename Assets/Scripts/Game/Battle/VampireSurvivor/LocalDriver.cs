/*--------------------------------------------------------------
 * File: LocalDriver.cs
 * Author: Wsw
 * Time: 2026/08/31
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单机战斗驱动器（吸血鬼幸存者本地固定步长）：
/// - 注册 CoreMgr.Update（IUpdateable），用墙钟时间（realtimeSinceStartup）累积毫秒预算，
///   每满 VampireLogic.LogicFrameMs(50ms) 推进一逻辑帧，帧号从 1 递增
/// - 帧边界采样输入后调 VampireLogic.Tick(frame, inputs)，形状与 FrameSyncMgr.OnFrame 对齐：
///   同一逻辑层既可被本驱动器驱动（单机），阶段 7 联机时换成 FrameSyncMgr 驱动，Logic/表现层零改动
/// - 暂停：直接停步进（不消费帧、不累积墙钟预算），不动 TimeScale；
///   恢复时不快进补帧（丢弃暂停期间流逝的墙钟）
///
/// 确定性纪律（驱动侧）：只决定"何时推进多少帧"，不参与逻辑演算；
/// 输入只在帧边界采样（阶段 1-4 由 IBattleInput 填充），逻辑层禁止读 Input.*
/// </summary>
public class LocalDriver : IUpdateable
{
    private readonly VampireLogic _logic;
    private double _lastRealTime = -1; // 上次采样的墙钟时间（-1 = 未初始化）
    private float _frameTimer;         // 墙钟累积的毫秒预算
    private int _nextFrameId = 1;      // 下一帧号（帧号从 1 递增）
    private readonly List<VsInput> _inputs = new();       // 帧内输入缓存（预分配，帧边界采样填充）
    private readonly List<IBattleInput> _inputSources = new(); // 已注册输入源（P1 摇杆等，按注册序采样）

    /// <summary>暂停状态（true = 逻辑步进停止，TimeScale 不受影响）</summary>
    public bool Paused { get; private set; }

    /// <summary>当前已推进的逻辑帧号（未开始为 0）</summary>
    public int CurFrameId { get; private set; }

    /// <summary>每推一帧触发一次（携带帧号），表现层在此做视图对账（对齐 FrameSyncMgr.OnFrame）</summary>
    public event Action<int> OnFrame;

    /// <summary>
    /// 每渲染帧触发一次（alpha = 当前逻辑帧推进进度 0~1，deltaSeconds = 本渲染帧真实秒数），
    /// 表现层据此做帧间插值与视觉动画（联机时 FrameSyncMgr 以消费节拍器进度提供同形状事件）
    /// </summary>
    public event Action<float, float> OnRenderFrame;

    public LocalDriver(VampireLogic logic)
    {
        _logic = logic;
        CoreMgr.Update.RegisterUpdate(this);
    }

    /// <summary>注册输入源（帧边界采样进 Tick；可多个，按注册序供给 PlayerIndex 对应槽位）</summary>
    public void RegisterInput(IBattleInput input)
    {
        _inputSources.Add(input);
    }

    public void MyUpdate(float deltaTime, float realDeltaTime)
    {
        if (Paused)
        {
            // 暂停：停步进（不消费帧/不累积预算），alpha 冻结，插值输出恒定（表现世界冻结）
            OnRenderFrame?.Invoke(Mathf.Clamp01(_frameTimer / VampireLogic.LogicFrameMs), 0f);
            return;
        }

        // 用墙钟时间累积（同 FrameSyncMgr）：编辑器失焦/长卡顿仍按真实流逝时间步进，
        // 预算 = 真实流逝时间，逻辑帧与墙钟同步
        var now = Time.realtimeSinceStartup;
        if (_lastRealTime < 0) _lastRealTime = now;
        _frameTimer += (float)((now - _lastRealTime) * 1000.0);
        var deltaSeconds = (float)(now - _lastRealTime); // 本渲染帧真实流逝秒数（表现层视觉动画用）
        _lastRealTime = now;

        while (_frameTimer >= VampireLogic.LogicFrameMs)
        {
            _frameTimer -= VampireLogic.LogicFrameMs;
            CurFrameId = _nextFrameId++;

            // 帧边界采样输入，再喂逻辑层
            SampleInputs();
            _logic.Tick(CurFrameId, _inputs);
            OnFrame?.Invoke(CurFrameId);
        }

        // 渲染帧插值驱动：alpha = 当前帧推进进度（追帧多帧时 prev/cur 仍为最后相邻两帧，插值平滑）
        OnRenderFrame?.Invoke(Mathf.Clamp01(_frameTimer / VampireLogic.LogicFrameMs), deltaSeconds);
    }

    /// <summary>
    /// 帧边界采样：遍历已注册输入源，产出帧内 VsInput 列表（无输入源 = 空帧）
    /// </summary>
    private void SampleInputs()
    {
        _inputs.Clear();
        for (int i = 0; i < _inputSources.Count; i++)
        {
            var src = _inputSources[i];
            src.Sample(out var dirX, out var dirY);
            _inputs.Add(new VsInput { PlayerIndex = src.PlayerIndex, DirX = dirX, DirY = dirY });
        }
    }

    /// <summary>暂停逻辑步进（直接停步进，不动 TimeScale）</summary>
    public void Pause()
    {
        Paused = true;
    }

    /// <summary>恢复逻辑步进。丢弃暂停期间流逝的墙钟，恢复后不追帧</summary>
    public void Resume()
    {
        Paused = false;
        _lastRealTime = -1; // 下一帧重新采样起点
    }

    /// <summary>注销 Update 订阅（战斗退出时由 VampireView.Dispose 调用）</summary>
    public void Dispose()
    {
        CoreMgr.Update.UnRegisterUpdate(this);
    }
}
