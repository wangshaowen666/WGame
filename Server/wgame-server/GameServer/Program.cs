using Google.Protobuf;
using LiteNetLib;

namespace GameServer;

/// <summary>
/// 帧同步游戏服务器：proto 信封收发（NetMsgEnvelope）
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        var listener = new EventBasedNetListener();

        // 客户端连接请求 → 接受
        listener.ConnectionRequestEvent += request =>
        {
            request.Accept();
            Console.WriteLine($"[连接请求] 接受客户端: {request.RemoteEndPoint}");
        };

        // 客户端连接成功
        listener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine($"[已连接] 客户端: {peer.Address}:{peer.Port}");
        };

        // 客户端断开
        listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
        {
            Console.WriteLine($"[已断开] 客户端: {peer.Address}:{peer.Port}, 原因: {disconnectInfo.Reason}");
        };

        // 收到客户端数据：解析 proto 信封并分发
        listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
        {
            var raw = reader.GetRemainingBytes();
            if (raw == null || raw.Length == 0)
            {
                reader.Recycle();
                return;
            }

            try
            {
                var envelope = NetMsg.NetMsgEnvelope.Parser.ParseFrom(raw);
                HandleEnvelope(peer, envelope);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[警告] 解析 proto 消息失败: {e.Message}");
            }

            reader.Recycle();
        };

        var netManager = new NetManager(listener)
        {
            AutoRecycle = true,
            DisconnectTimeout = 10000,
        };

        const int port = 7777;
        if (!netManager.Start(port))
        {
            Console.WriteLine($"服务器启动失败（端口可能被占用）: {port}");
            return;
        }
        Console.WriteLine($"服务器已启动，监听 UDP 端口 {port}");
        Console.WriteLine("等待客户端连接...（按 Ctrl+C 退出）");

        // 主循环：轮询网络事件
        while (true)
        {
            netManager.PollEvents();
            Thread.Sleep(15);
        }
    }

    /// <summary>
    /// 按信封 msg_type 分发处理
    /// </summary>
    private static void HandleEnvelope(NetPeer peer, NetMsg.NetMsgEnvelope envelope)
    {
        Console.WriteLine($"[收到数据] 来自 {peer.Address}:{peer.Port}, 消息类型: {envelope.MsgType}");

        switch (envelope.MsgType)
        {
            case NetMsg.MsgType.MsgPlayerInput:
                var input = NetMsg.PlayerInput.Parser.ParseFrom(envelope.Payload);
                Console.WriteLine($"[玩家操作] op_type={input.OpType}, param1={input.Param1}, param3={input.Param3}");
                break;

            default:
                Console.WriteLine($"[警告] 未知消息类型: {envelope.MsgType}");
                break;
        }
    }
}
