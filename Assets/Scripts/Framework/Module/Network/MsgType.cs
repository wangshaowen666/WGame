/*--------------------------------------------------------------
 * File: MsgType.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/14 10:30:00 
 *--------------------------------------------------------------
 */

/// <summary>
/// 消息类型（帧同步协议，首字节）
/// 需与服务器 GameServer/MsgType.cs 保持一致
/// </summary>
public enum MsgType : byte
{
    // ===== 客户端 → 服务器 =====
    /// <summary>客户端问候（测试用，验证收发链路）</summary>
    C2S_Hello = 1,

    // ===== 服务器 → 客户端 =====
    /// <summary>服务器回复问候</summary>
    S2C_HelloAck = 2,
}
