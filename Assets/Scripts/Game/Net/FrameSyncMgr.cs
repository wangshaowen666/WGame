/*--------------------------------------------------------------
 * File: FrameSyncMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/18 10:00:00
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;

/// <summary>
/// 帧同步管理器（热更）：
/// - 发送：业务层调 SendInput 立即发给服务器，由服务器攒批进帧
/// - 接收：收到 FrameData 入缓冲队列
/// - 驱动：墙钟累积时间，每满 50ms 消费一帧缓冲；
///   缓冲堆积时自动快进连续消费追平（积压帧数与停顿时长产生的预算恰好相等，债务自清），
///   缓冲不足 2 帧时暂停消费（防网络抖动）
/// 业务逻辑（战斗等）订阅 OnFrame，在回调中确定性执行，禁止使用本地未同步状态驱动
/// </summary>
public class FrameSyncMgr : ManagerBase, IUpdateable
{
    /// <summary>逻辑帧间隔（毫秒），必须与服务器 LogicFrameMs 一致</summary>
    public const int LogicFrameMs = 50;

    /// <summary>缓冲帧数：缓冲不足此值时暂停消费，网络抖动时逻辑帧不空转</summary>
    public const int BufferFrames = 2;

    private readonly Queue<NetMsg.FrameData> _frameBuffer = new();
    private float _frameTimer;
    private double _lastRealTime = -1; // 上次采样的墙钟时间（-1 = 未初始化）
    private int _nextFrameId; // 期望的下一个帧号（0 = 尚未收到首帧）

    /// <summary>当前已执行的逻辑帧号（未开始为 0）</summary>
    public int CurFrameId { get; private set; }

    /// <summary>当前缓冲帧数（调试用）</summary>
    public int BufferCount => _frameBuffer.Count;

    /// <summary>每消费一帧触发一次（inputs 可能为空帧），业务层在此驱动确定性逻辑</summary>
    public event Action<NetMsg.FrameData> OnFrame;

    public override void OnInit()
    {
        GameMgr.NetMsg.OnFrameData += EnqueueFrame;
        CoreMgr.Net.OnDisconnected += OnDisconnected;
        CoreMgr.Update.RegisterUpdate(this);
    }

    /// <summary>
    /// 发送玩家操作（服务器攒批进最近的逻辑帧后广播给所有客户端）
    /// </summary>
    /// <param name="opType">操作类型：1=放置塔 2=升级塔</param>
    /// <param name="param1">参数1（如格子 x）</param>
    /// <param name="param2">参数2（如格子 y）</param>
    public void SendInput(int opType, int param1 = 0, int param2 = 0)
    {
        GameMgr.NetMsg.Send(NetMsg.MsgType.MsgPlayerInput, new NetMsg.PlayerInput
        {
            OpType = opType,
            Param1 = param1,
            Param2 = param2,
        });
    }

    /// <summary>
    /// 收到服务器帧：入缓冲队列，等待逻辑帧驱动消费
    /// </summary>
    private void EnqueueFrame(NetMsg.FrameData frame)
    {
        _frameBuffer.Enqueue(frame);
    }

    /// <summary>
    /// 断线：清空缓冲与帧号状态（重连后重新从服务器当前帧开始）
    /// </summary>
    private void OnDisconnected()
    {
        _frameBuffer.Clear();
        _frameTimer = 0;
        _nextFrameId = 0;
        CurFrameId = 0;
    }

    /// <summary>
    /// 逻辑帧驱动：墙钟累积时间，每满 50ms 消费一帧缓冲；
    /// 缓冲堆积时自动快进连续消费追平，缓冲不足 2 帧时暂停消费（防抖动）
    /// </summary>
    public void MyUpdate(float deltaTime, float realDeltaTime)
    {
        // 用墙钟时间（realtimeSinceStartup）累积，而非 deltaTime：
        // deltaTime 受 timeScale / maximumDeltaTime 钳制，编辑器失焦暂停或长卡顿会"丢时间"，
        // 导致消费欠账（缓冲堆积出恒定延迟）；墙钟保证积累量=真实流逝时间。
        var now = UnityEngine.Time.realtimeSinceStartup;
        if (_lastRealTime < 0) _lastRealTime = now;
        _frameTimer += (float)((now - _lastRealTime) * 1000.0);
        _lastRealTime = now;

        while (_frameTimer >= LogicFrameMs)
        {
            // 缓冲不足：暂停消费等待网络；计时钳制为一帧的量（缓冲恢复后立即消费，不快进）
            if (_frameBuffer.Count < BufferFrames)
            {
                if (_frameTimer > LogicFrameMs)
                    _frameTimer = LogicFrameMs;
                break;
            }

            _frameTimer -= LogicFrameMs;
            var frame = _frameBuffer.Dequeue();

            // 帧号连续性检测：ReliableOrdered 下不应发生，发生说明实现有异常
            if (_nextFrameId != 0 && frame.FrameId != _nextFrameId)
                Log.Warning("帧号不连续, 期望:", _nextFrameId, "实际:", frame.FrameId);
            _nextFrameId = frame.FrameId + 1;

            CurFrameId = frame.FrameId;
            OnFrame?.Invoke(frame);
        }
    }

    /// <summary>在 NetMgr(100) 之后轮询：先收完网络数据，再消费帧</summary>
    public int Priority => 90;

    public override void OnSceneExit(int sceneTp)
    {
        // _frameBuffer.Clear();
        // _frameTimer = 0;
    }
}
