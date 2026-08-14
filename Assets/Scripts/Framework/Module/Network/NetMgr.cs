/*--------------------------------------------------------------
 * File: NetMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/14 10:00:00 
 *--------------------------------------------------------------
 */

using System;
using LiteNetLib;
using LiteNetLib.Utils;

/// <summary>
/// 实时网络管理：连接服务器、心跳保活、断线检测。
/// 任务2：仅实现连接/断开/断线检测，消息协议在任务3实现
/// </summary>
public class NetMgr : ManagerBase, IUpdateable
{
    private NetManager _netManager;
    private NetPeer _serverPeer;

    /// <summary>是否已连接服务器</summary>
    public bool IsConnected =>
        _serverPeer != null && _serverPeer.ConnectionState == ConnectionState.Connected;

    public event Action OnConnected;
    public event Action OnDisconnected;

    /// <summary>
    /// 连接服务器
    /// </summary>
    public void Connect(string ip, int port)
    {
        var listener = new EventBasedNetListener();

        // 连接成功（客户端视角）
        listener.PeerConnectedEvent += peer =>
        {
            _serverPeer = peer;
            Log.Info("已连接服务器:", peer.Address, peer.Port);
            OnConnected?.Invoke();
        };

        // 断开（含超时/主动断开）
        listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
        {
            _serverPeer = null;
            Log.Warning("与服务器断开:", disconnectInfo.Reason);
            OnDisconnected?.Invoke();
        };

        // 收到数据：解析协议并处理
        listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
        {
            // 首字节是消息类型
            var msgType = (MsgType)reader.GetByte();
            Log.Info("收到服务器消息:", msgType);

            switch (msgType)
            {
                case MsgType.S2C_HelloAck:
                    var text = reader.GetString();
                    Log.Info("服务器回复:", text);
                    break;

                default:
                    Log.Warning("未知消息类型:", msgType);
                    break;
            }

            reader.Recycle();
        };

        _netManager = new NetManager(listener)
        {
            AutoRecycle = true,
            // 心跳保活：LiteNetLib 内置 ping 机制，超过该时长未收到对方数据则判定断线
            DisconnectTimeout = 10000,
        };

        _netManager.Start();
        // connectKey：连接令牌（任务1服务器未校验，任务5匹配时再校验身份）
        _netManager.Connect(ip, port, "wgame");
        Log.Info("正在连接服务器:", ip, port);

        // 注册到 UpdateMgr，每帧轮询网络事件
        CoreMgr.Update.RegisterUpdate(this);
    }

    /// <summary>
    /// 主动断开连接
    /// </summary>
    public void Disconnect()
    {
        _netManager?.Stop();
        _netManager = null;
        _serverPeer = null;
        CoreMgr.Update.UnRegisterUpdate(this);
    }

    /// <summary>
    /// 发送问候消息（测试收发链路用）
    /// </summary>
    public void SendHello(string name)
    {
        if (!IsConnected)
        {
            Log.Error("未连接服务器，无法发送消息");
            return;
        }

        var writer = new NetDataWriter();
        writer.Put((byte)MsgType.C2S_Hello);
        writer.Put(name);
        _serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        Log.Info("已发送问候:", name);
    }

    /// <summary>
    /// 每帧轮询网络事件（由 UpdateMgr 驱动）
    /// </summary>
    public void MyUpdate(float deltaTime, float realDeltaTime)
    {
        _netManager?.PollEvents();
    }

    public int Priority => 100;

    /// <summary>
    /// 场景退出时断开连接
    /// </summary>
    public override void OnSceneExit(int sceneTp)
    {
        Disconnect();
    }
}
