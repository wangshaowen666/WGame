using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Protobuf;
using LiteNetLib;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GameServer;

/// <summary>
/// 帧同步游戏服务器：
/// - proto 信封收发（NetMsgEnvelope）
/// - UDP 连接认证：客户端连上后发 UdpLoginReq（HTTP 登录获取的 JWT token），
///   服务器用与 LoginServer 相同的 Jwt:Secret 校验并解析 playerId 绑定连接；
///   未认证连接的 PlayerInput 直接丢弃（无法伪造身份参与游戏）
/// - 逻辑帧循环：每 50ms 把期间收到的所有玩家操作打包成一帧广播（空帧也发，保证帧号连续）
/// - 阶段 5 暂无房间概念（阶段 6 实现），所有在线客户端视为同一场游戏
/// </summary>
public static class Program
{
    private const int LogicFrameMs = 50;     // 逻辑帧间隔（20 帧/秒），需与客户端 FrameSyncMgr 一致
    private const int MaxOpType = 2;         // 合法操作类型上限（1=放置塔 2=升级塔，0 已废弃）
    private const int MaxInputsPerFrame = 8; // 每玩家每帧最多操作数（防刷）

    private static NetManager _netManager = null!;
    private static string _jwtSecret = null!;

    /// <summary>连接 -> 认证后的 playerId（未认证的连接不在表内）</summary>
    private static readonly Dictionary<NetPeer, int> _peerPlayers = new();

    public static void Main(string[] args)
    {
        // 加载配置（Jwt:Secret 须与 LoginServer 一致，否则 token 校验必然失败）
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        _jwtSecret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("缺少配置 Jwt:Secret");

        var listener = new EventBasedNetListener();

        // 客户端连接请求 -> 接受（认证在其后通过 UdpLoginReq 完成）
        listener.ConnectionRequestEvent += request =>
        {
            request.Accept();
            Console.WriteLine($"[连接请求] 接受客户端: {request.RemoteEndPoint}");
        };

        // 客户端连接成功
        listener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine($"[已连接] 客户端: {peer.Address}:{peer.Port}, 等待认证...");
        };

        // 客户端断开
        listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
        {
            var hadAuth = _peerPlayers.TryGetValue(peer, out var pid);
            _peerPlayers.Remove(peer);

            // 断线：出匹配队列 + 自动退房（剩余成员会收到 RoomStatePush）
            MatchMaker.Dequeue(peer);
            RoomMgr.RemoveFromRoom(peer);

            Console.WriteLine($"[已断开] playerId={(hadAuth ? pid : -1)}, 原因: {disconnectInfo.Reason}");
        };

        // 收到客户端数据：解析 proto 信封并分发
        listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
        {
            var raw = reader.GetRemainingBytes();
            reader.Recycle();
            if (raw == null || raw.Length == 0) return;

            try
            {
                HandleEnvelope(peer, NetMsg.NetMsgEnvelope.Parser.ParseFrom(raw));
            }
            catch (Exception e)
            {
                Console.WriteLine($"[警告] 解析 proto 消息失败: {e.Message}");
            }
        };

        _netManager = new NetManager(listener)
        {
            AutoRecycle = true,
            DisconnectTimeout = 10000,
        };

        const int port = 7777;
        if (!_netManager.Start(port))
        {
            Console.WriteLine($"服务器启动失败（端口可能被占用）: {port}");
            return;
        }
        Console.WriteLine($"服务器已启动，监听 UDP 端口 {port}，逻辑帧 {LogicFrameMs}ms/帧（{1000 / LogicFrameMs} 帧/秒）");
        Console.WriteLine("等待客户端连接...（按 Ctrl+C 退出）");

        // 主循环：轮询网络事件 + 固定步长逻辑帧（累加器模式，业内通用做法）
        // 相比 Restart() 计时：处理耗时不被丢弃（时间进累加器），帧率更稳；
        // 相比 Sleep(1) 忙等：睡到距下一帧边界最多 5ms 的小片，既省 CPU 又保证网络事件及时处理
        var clock = Stopwatch.StartNew();
        long lastTicks = clock.ElapsedTicks;
        double accumulatorMs = 0;
        const double MaxCatchUpMs = LogicFrameMs * 3; // 钳制累积量：最多补 3 帧，防卡顿后死亡螺旋
        while (true)
        {
            _netManager.PollEvents();

            // 累积流逝时间（Stopwatch 高精度单调时钟，不受系统改时间影响）
            var nowTicks = clock.ElapsedTicks;
            accumulatorMs += (nowTicks - lastTicks) * 1000.0 / Stopwatch.Frequency;
            lastTicks = nowTicks;

            if (accumulatorMs > MaxCatchUpMs)
                accumulatorMs = MaxCatchUpMs;

            // 固定步长消费：超时补帧（钳制下最多 3 帧）
            while (accumulatorMs >= LogicFrameMs)
            {
                accumulatorMs -= LogicFrameMs;
                BroadcastFrame();
            }

            // 分片睡眠：距下一帧边界还有多少睡多少（单片上限 5ms），期间保持轮询网络
            var remainMs = LogicFrameMs - accumulatorMs;
            var sleepMs = (int)Math.Min(remainMs, 5);
            Thread.Sleep(Math.Max(0, sleepMs));
        }
    }

    /// <summary>
    /// 按信封 msg_type 分发处理
    /// </summary>
    private static void HandleEnvelope(NetPeer peer, NetMsg.NetMsgEnvelope envelope)
    {
        switch (envelope.MsgType)
        {
            case NetMsg.MsgType.MsgUdpLoginReq:
                OnUdpLogin(peer, NetMsg.UdpLoginReq.Parser.ParseFrom(envelope.Payload));
                break;

            case NetMsg.MsgType.MsgPlayerInput:
                OnPlayerInput(peer, NetMsg.PlayerInput.Parser.ParseFrom(envelope.Payload));
                break;

            case NetMsg.MsgType.MsgRoomReq:
                OnRoomReq(peer, NetMsg.RoomReq.Parser.ParseFrom(envelope.Payload));
                break;

            case NetMsg.MsgType.MsgLeaveRoomReq:
                OnLeaveRoom(peer);
                break;

            case NetMsg.MsgType.MsgMatchReq:
                OnMatchReq(peer, NetMsg.MatchReq.Parser.ParseFrom(envelope.Payload));
                break;

            case NetMsg.MsgType.MsgReadyReq:
                OnReadyReq(peer);
                break;

            default:
                Console.WriteLine($"[警告] 未知消息类型: {envelope.MsgType}");
                break;
        }
    }

    /// <summary>
    /// 就绪请求：在房间内且未开局则标记就绪；全员就绪由 Room.SetReady 触发开战推送
    /// </summary>
    private static void OnReadyReq(NetPeer peer)
    {
        if (!RoomMgr.PeerRooms.TryGetValue(peer, out var room))
        {
            SendTo(peer, NetMsg.MsgType.MsgReadyResp, new NetMsg.ReadyResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorNotInRoom,
            });
            return;
        }

        if (room.IsStarted)
        {
            SendTo(peer, NetMsg.MsgType.MsgReadyResp, new NetMsg.ReadyResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorAlreadyInRoom,
            });
            return;
        }

        var playerId = _peerPlayers.TryGetValue(peer, out var pid) ? pid : 0;
        Console.WriteLine($"[房间{room.Id}] playerId={playerId} 就绪");
        room.SetReady(peer);

        SendTo(peer, NetMsg.MsgType.MsgReadyResp, new NetMsg.ReadyResp
        {
            ErrorCode = NetMsg.ErrorCode.ErrorNone,
        });
    }

    /// <summary>
    /// 匹配请求：开始匹配（入队，队列满 2 人时撮合）/ 取消匹配（出队）
    /// </summary>
    private static void OnMatchReq(NetPeer peer, NetMsg.MatchReq req)
    {
        // 前置校验：已认证
        if (!_peerPlayers.TryGetValue(peer, out var playerId))
        {
            SendTo(peer, NetMsg.MsgType.MsgMatchResp, new NetMsg.MatchResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorUnauthorized,
            });
            return;
        }

        if (req.Cancel)
        {
            // 取消匹配
            MatchMaker.Dequeue(peer);
            Console.WriteLine($"[匹配] playerId={playerId} 取消匹配");
            SendTo(peer, NetMsg.MsgType.MsgMatchResp, new NetMsg.MatchResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorNone,
                MatchState = NetMsg.MatchState.Cancelled,
            });
            return;
        }

        // 前置校验：不在房间（匹配的目的就是进房，已进房则拒绝）
        if (RoomMgr.IsInRoom(peer))
        {
            SendTo(peer, NetMsg.MsgType.MsgMatchResp, new NetMsg.MatchResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorAlreadyInRoom,
            });
            return;
        }

        // 入队并尝试撮合（撮合成功时 MatchMaker 会直接回 MATCHED 的 MatchResp）
        MatchMaker.Enqueue(peer, playerId);
        // 受理回执（若已撮合，客户端会先收到 MATCHED，此回执只是补充）
        SendTo(peer, NetMsg.MsgType.MsgMatchResp, new NetMsg.MatchResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorNone,
                MatchState = NetMsg.MatchState.Matching,
            });
    }

    /// <summary>
    /// 房间请求：room_id=0 创建新房间；room_id>0 加入指定房间。
    /// 前置条件：已通过 UDP 认证且当前不在房间中
    /// </summary>
    private static void OnRoomReq(NetPeer peer, NetMsg.RoomReq req)
    {
        // 前置校验：已认证
        if (!_peerPlayers.TryGetValue(peer, out var playerId))
        {
            SendTo(peer, NetMsg.MsgType.MsgRoomResp, new NetMsg.RoomResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorUnauthorized,
            });
            return;
        }

        // 前置校验：未在其他房间
        if (RoomMgr.IsInRoom(peer))
        {
            Console.WriteLine($"[校验失败] playerId={playerId} 已在房间，拒绝建房/加房");
            SendTo(peer, NetMsg.MsgType.MsgRoomResp, new NetMsg.RoomResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorAlreadyInRoom,
            });
            return;
        }

        if (req.RoomId == 0)
        {
            // 创建新房间
            var room = RoomMgr.CreateRoom();
            RoomMgr.JoinRoom(room, peer, playerId);
            Console.WriteLine($"[房间{room.Id}] 创建并加入: playerId={playerId}");
            SendTo(peer, NetMsg.MsgType.MsgRoomResp, new NetMsg.RoomResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorNone,
                RoomId = room.Id,
            });
            room.PushState();
        }
        else
        {
            // 加入指定房间
            if (!RoomMgr.Rooms.TryGetValue(req.RoomId, out var room))
            {
                SendTo(peer, NetMsg.MsgType.MsgRoomResp, new NetMsg.RoomResp
                {
                    ErrorCode = NetMsg.ErrorCode.ErrorRoomNotFound,
                });
                return;
            }

            if (room.IsFull)
            {
                SendTo(peer, NetMsg.MsgType.MsgRoomResp, new NetMsg.RoomResp
                {
                    ErrorCode = NetMsg.ErrorCode.ErrorRoomFull,
                });
                return;
            }

            RoomMgr.JoinRoom(room, peer, playerId);
            Console.WriteLine($"[房间{room.Id}] 加入: playerId={playerId}, 当前 {room.MemberCount} 人");
            SendTo(peer, NetMsg.MsgType.MsgRoomResp, new NetMsg.RoomResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorNone,
                RoomId = room.Id,
            });
            room.PushState();
        }
    }

    /// <summary>
    /// 退出房间：成员变化推送剩余成员；房间清空则销毁
    /// </summary>
    private static void OnLeaveRoom(NetPeer peer)
    {
        if (RoomMgr.RemoveFromRoom(peer) == null)
        {
            SendTo(peer, NetMsg.MsgType.MsgLeaveRoomResp, new NetMsg.LeaveRoomResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorNotInRoom,
            });
            return;
        }

        SendTo(peer, NetMsg.MsgType.MsgLeaveRoomResp, new NetMsg.LeaveRoomResp
        {
            ErrorCode = NetMsg.ErrorCode.ErrorNone,
        });
    }

    /// <summary>
    /// UDP 连接认证：校验 JWT（签名 + 过期），解析 playerId 绑定连接
    /// </summary>
    private static void OnUdpLogin(NetPeer peer, NetMsg.UdpLoginReq req)
    {
        if (ValidateToken(req.Token, out var playerId))
        {
            _peerPlayers[peer] = playerId;
            Console.WriteLine($"[认证成功] playerId={playerId} ({peer.Address}:{peer.Port})");
            SendTo(peer, NetMsg.MsgType.MsgUdpLoginResp, new NetMsg.UdpLoginResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorNone,
                PlayerId = playerId,
            });
        }
        else
        {
            Console.WriteLine($"[认证失败] token 无效或已过期 ({peer.Address}:{peer.Port})");
            SendTo(peer, NetMsg.MsgType.MsgUdpLoginResp, new NetMsg.UdpLoginResp
            {
                ErrorCode = NetMsg.ErrorCode.ErrorUnauthorized,
            });
            // TODO（阶段 6 后续）：认证失败次数超限踢下线，防刷包
        }
    }

    /// <summary>
    /// 校验 JWT 并解析 playerId（口径与 LoginServer JwtBearer 中间件一致）
    /// </summary>
    private static bool ValidateToken(string token, out int playerId)
    {
        playerId = 0;
        if (string.IsNullOrEmpty(token)) return false;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
                ClockSkew = TimeSpan.FromSeconds(30),
            }, out _);

            var idStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idStr, out playerId);
        }
        catch (Exception)
        {
            return false; // 签名不符/格式错误/过期等，一律视为无效
        }
    }

    /// <summary>
    /// 发送 proto 消息到指定连接（自动包信封）。MatchMaker 复用，须 internal
    /// </summary>
    internal static void SendTo(NetPeer peer, NetMsg.MsgType msgType, IMessage msg)
    {
        var envelope = new NetMsg.NetMsgEnvelope
        {
            MsgType = msgType,
            Payload = msg.ToByteString(),
        };
        peer.Send(envelope.ToByteArray(), DeliveryMethod.ReliableOrdered);
    }

    /// <summary>取连接绑定的 playerId（未认证返回 0，MatchMaker 撮合时复用）</summary>
    internal static int GetPlayerId(NetPeer peer)
        => _peerPlayers.TryGetValue(peer, out var id) ? id : 0;

    /// <summary>
    /// 收到玩家操作：校验（已认证 + 在房间内 + 操作合法）后攒进所在房间
    /// </summary>
    private static void OnPlayerInput(NetPeer peer, NetMsg.PlayerInput input)
    {
        // 校验 0：必须先通过 UDP 认证（未认证连接的操作一律丢弃）
        if (!_peerPlayers.TryGetValue(peer, out var playerId))
        {
            Console.WriteLine($"[校验失败] 未认证连接 ({peer.Address}:{peer.Port}) 的操作，丢弃");
            return;
        }

        // 校验 1：必须先进入房间（未进房的操作无广播对象，丢弃）
        if (!RoomMgr.PeerRooms.TryGetValue(peer, out var room))
        {
            Console.WriteLine($"[校验失败] playerId={playerId} 不在房间内，操作丢弃");
            return;
        }

        // 校验 2：操作类型合法（非法 op_type 直接丢弃，防协议滥用）
        if (input.OpType < 0 || input.OpType > MaxOpType)
        {
            Console.WriteLine($"[校验失败] playerId={playerId} 非法 op_type={input.OpType}，丢弃");
            return;
        }

        // 校验 3：单帧操作数防刷（正常操作远达不到该频率，超限视为刷包）
        if (room.CountInputsOf(playerId) >= MaxInputsPerFrame)
        {
            Console.WriteLine($"[校验失败] playerId={playerId} 单帧操作数超过 {MaxInputsPerFrame}，丢弃");
            return;
        }

        // player_id 由服务器填充（token 解析的真实 playerId），客户端无法伪造身份
        room.AddInput(new NetMsg.PlayerInput
        {
            OpType = input.OpType,
            Param1 = input.Param1,
            Param2 = input.Param2,
            PlayerId = playerId,
        });
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}][玩家操作] 房间{room.Id} playerId={playerId}, op_type={input.OpType}, param1={input.Param1}, param2={input.Param2}");
    }

    /// <summary>
    /// 广播一帧：遍历所有房间，各房间独立帧号打包自己的攒批操作发成员
    /// （空帧也发，保证帧号连续；房间空了就地销毁）
    /// </summary>
    private static void BroadcastFrame()
    {
        // 遍历中销毁：先收集要移除的房间号
        List<int>? emptyRooms = null;
        foreach (var kv in RoomMgr.Rooms)
        {
            if (kv.Value.BroadcastFrame())
            {
                emptyRooms ??= new List<int>();
                emptyRooms.Add(kv.Key);
            }
        }

        if (emptyRooms != null)
        {
            foreach (var id in emptyRooms)
                RoomMgr.Rooms.Remove(id);
        }
    }
}
