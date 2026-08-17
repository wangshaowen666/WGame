/*--------------------------------------------------------------
 * File: TTTT.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/12 18:09:09 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using Unity.Profiling;
using UnityEngine;

public class TTTT : MonoBehaviour
{
    [ContextMenu("执行")]
    void Test()
    {
        CoreMgr.Net.Connect("127.0.0.1", 7777);
    }

    [ContextMenu("发送问候")]
    void SendHello()
    {
        var input = new NetMsg.PlayerInput { OpType = 1, Param1 = 100, Param3 = 200 };
        GameMgr.NetMsg.Send(NetMsg.MsgType.MsgPlayerInput, input);
    }

    [ContextMenu("proto序列化自测")]
    void ProtoTest()
    {
        var input = new NetMsg.PlayerInput { OpType = 1, Param1 = 100, Param3 = 200 };
        byte[] bytes = input.ToByteArray();
        var parsed = NetMsg.PlayerInput.Parser.ParseFrom(bytes);
        Log.Info("proto自测: 序列化", bytes.Length, "字节, 反序列化结果: op_type=", parsed.OpType, "param1=", parsed.Param1, "param2=", parsed.Param3);
    }
    
    // 测试我的自定义转换

    private void A()
    {
        for (int i = 0; i < 100; i++)
        {
            // VarInt b = i;
            // N(b);
            object c = i;
            M(c);
        }
    }

    private void M(object o)
    {
        int i = 10;
    }
    
    private void N(Variable o)
    {
        //ClassPool.Recycle(o);
    }
}

public class C
{
    //public string name = "hah";
}