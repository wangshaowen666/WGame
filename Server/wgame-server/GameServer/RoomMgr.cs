using LiteNetLib;

namespace GameServer;

/// <summary>
/// 房间管理器（服务器侧）：房间字典与连接归属的统一入口。
/// 从 Program.cs 收敛而来，MatchMaker 等逻辑复用此入口，避免直接操作字典
/// </summary>
public static class RoomMgr
{
    /// <summary>房间号 -> 房间</summary>
    public static Dictionary<int, Room> Rooms { get; } = new();

    /// <summary>连接 -> 所在房间</summary>
    public static Dictionary<NetPeer, Room> PeerRooms { get; } = new();

    private static int _nextRoomId;

    /// <summary>创建房间（不加入成员）</summary>
    public static Room CreateRoom()
    {
        var room = new Room(++_nextRoomId);
        Rooms[room.Id] = room;
        return room;
    }

    /// <summary>加入房间（未校验前置条件，调用方保证已认证/不在房/房间未满）</summary>
    public static void JoinRoom(Room room, NetPeer peer, int playerId)
    {
        room.Join(peer, playerId);
        PeerRooms[peer] = room;
    }

    /// <summary>连接是否已在房间</summary>
    public static bool IsInRoom(NetPeer peer) => PeerRooms.ContainsKey(peer);

    /// <summary>
    /// 从房间移除连接（主动退出/断线共用）：推送剩余成员，房间空则销毁。
    /// 返回所在房间（供调用方打日志），未在房间时返回 null
    /// </summary>
    public static Room? RemoveFromRoom(NetPeer peer)
    {
        if (!PeerRooms.TryGetValue(peer, out var room)) return null;

        room.Remove(peer);
        PeerRooms.Remove(peer);
        Console.WriteLine($"[房间{room.Id}] 移除: 剩余 {room.MemberCount} 人");

        if (room.MemberCount == 0)
        {
            Rooms.Remove(room.Id);
            Console.WriteLine($"[房间{room.Id}] 已空，销毁");
            return room;
        }

        room.PushState();
        return room;
    }
}
