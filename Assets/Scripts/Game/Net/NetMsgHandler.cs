/*--------------------------------------------------------------
 * File: NetMsgHandler.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/15 10:00:00 
 *--------------------------------------------------------------
 */

using System;
using Google.Protobuf;

/// <summary>
/// 网络协议处理层（热更）：
/// 订阅 Framework 的 NetMgr 传输层，负责协议消息的封装/解析/分发。
/// 新增协议类型：在对应 .proto 加 message + MsgType 枚举值，这里加 case 分支即可
/// </summary>
public class NetMsgHandler : ManagerBase
{
    public override void OnInit()
    {
        // 订阅传输层的原始数据事件
        CoreMgr.Net.OnRawData += OnRawDataReceived;
    }

    /// <summary>
    /// 发送 proto 消息（自动包成信封交给传输层）
    /// </summary>
    /// <typeparam name="T">proto 消息类型</typeparam>
    /// <param name="msgType">消息类型（NetMsg.MsgType 枚举）</param>
    /// <param name="msg">消息内容</param>
    public void Send<T>(NetMsg.MsgType msgType, T msg) where T : IMessage
    {
        var envelope = new NetMsg.NetMsgEnvelope
        {
            MsgType = msgType,
            Payload = msg.ToByteString(),
        };
        CoreMgr.Net.SendRaw(envelope.ToByteArray());
    }

    /// <summary>
    /// 收到原始字节：解析信封并按类型分发
    /// </summary>
    private void OnRawDataReceived(byte[] raw)
    {
        try
        {
            var envelope = NetMsg.NetMsgEnvelope.Parser.ParseFrom(raw);
            HandleEnvelope(envelope);
        }
        catch (Exception e)
        {
            Log.Error("解析 proto 消息失败:", e.Message);
        }
    }

    /// <summary>
    /// 按 msg_type 分发处理（示例处理 PlayerInput）
    /// </summary>
    private void HandleEnvelope(NetMsg.NetMsgEnvelope envelope)
    {
        Log.Info("收到服务器消息, 类型:", envelope.MsgType);

        switch (envelope.MsgType)
        {
            case NetMsg.MsgType.MsgPlayerInput:
                var input = NetMsg.PlayerInput.Parser.ParseFrom(envelope.Payload);
                Log.Info("收到玩家操作: op_type=", input.OpType, "param1=", input.Param1, "param3=", input.Param3);
                break;

            default:
                Log.Warning("未知消息类型:", envelope.MsgType);
                break;
        }
    }

    public override void OnSceneExit(int sceneTp)
    {
        // 取消订阅，避免泄漏
        CoreMgr.Net.OnRawData -= OnRawDataReceived;
    }
}
