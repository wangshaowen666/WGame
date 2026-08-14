using Google.Protobuf;
using LiteNetLib;
using LiteNetLib.Utils;

namespace GameServer;

/// <summary>
/// 帧同步游戏服务器（任务1：仅实现连接监听）
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        // ===== proto 序列化自测：验证生成代码可用 =====
        var hello = new Net.HelloMsg { Name = "测试玩家", Times = 1 };
        byte[] bytes = hello.ToByteArray();            // 序列化
        var parsed = Net.HelloMsg.Parser.ParseFrom(bytes); // 反序列化
        Console.WriteLine($"[proto自测] 序列化 {bytes.Length} 字节, 反序列化结果: name={parsed.Name}, times={parsed.Times}");

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

        // 收到客户端数据：解析协议并处理
        listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
        {
            // 首字节是消息类型
            var msgType = (MsgType)reader.GetByte();
            Console.WriteLine($"[收到数据] 来自 {peer.Address}:{peer.Port}, 消息类型: {msgType}");

            switch (msgType)
            {
                case MsgType.C2S_Hello:
                    // 读取客户端发送的名字
                    var name = reader.GetString();
                    Console.WriteLine($"[问候] 客户端 {peer.Address}:{peer.Port} 说: {name}");

                    // 回复 S2C_HelloAck
                    var writer = new NetDataWriter();
                    writer.Put((byte)MsgType.S2C_HelloAck);
                    writer.Put($"你好，{name}！服务器已收到你的消息");
                    peer.Send(writer, DeliveryMethod.ReliableOrdered);
                    Console.WriteLine($"[回复] 已发送问候确认给 {peer.Address}:{peer.Port}");
                    break;

                default:
                    Console.WriteLine($"[警告] 未知消息类型: {msgType}");
                    break;
            }

            reader.Recycle();
        };

        var netManager = new NetManager(listener)
        {
            AutoRecycle = true,
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
}
