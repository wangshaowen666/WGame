/*--------------------------------------------------------------
 * File: Log.cs
 * Author: Wang ShaoWen
 * Time: 2024/03/20 15:46:16 
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class Log 
{
    public enum LogColor
    {
        White,
        Red,
        Yellow,
        Green,
        Cyan,
        Orange,     
        Gray,
    }
    
    [Conditional("LOG_ON")]
    public static void Info(params object[] msgs)
    {
        Debug.Log(DealMsg(false, msgs));
    }
    
    [Conditional("LOG_ON")]
    public static void Info(LogColor color, params object[] msgs)
    {
        var msg = DealMsg(false, msgs);
        Debug.Log(string.Format("<color={0}>{1}</color>", color, msg));
    }
    
    [Conditional("LOG_ON")]
    public static void InfoLine(params object[] msgs)
    {
        Debug.Log(DealMsg(true, msgs));
    }
    
    [Conditional("LOG_ON")]
    public static void InfoLine(LogColor color, params object[] msgs)
    {
        var msg = DealMsg(true, msgs);
        Debug.Log(string.Format("<color={0}>{1}</color>", color, msg));
    }
    
    [Conditional("LOG_ON")]
    public static void Warning(params object[] msgs)
    {
        Debug.LogWarning(DealMsg(false, msgs));
    }
    
    [Conditional("LOG_ON")]
    public static void Warning(LogColor color, params object[] msgs)
    {
        var msg = DealMsg(false, msgs);
        Debug.LogWarning(string.Format("<color={0}>{1}</color>", color, msg));
    }
    
    [Conditional("LOG_ON")]
    public static void WarningLine(params object[] msgs)
    {
        Debug.LogWarning(DealMsg(true, msgs));
    }
    
    [Conditional("LOG_ON")]
    public static void WarningLine(LogColor color, params object[] msgs)
    {
        var msg = DealMsg(true, msgs);
        Debug.LogWarning(string.Format("<color={0}>{1}</color>", color, msg));
    }
    
    [Conditional("LOG_ON")]
    public static void Error(params object[] msgs)
    {
        Debug.LogError(DealMsg(false, msgs));
    }
    
    [Conditional("LOG_ON")]
    public static void Error(LogColor color, params object[] msgs)
    {
        var msg = DealMsg(false, msgs);
        Debug.LogError(string.Format("<color={0}>{1}</color>", color, msg));
    }
    
    [Conditional("LOG_ON")]
    public static void ErrorLine(params object[] msgs)
    {
        Debug.LogError(DealMsg(true, msgs));
    }
    
    [Conditional("LOG_ON")]
    public static void ErrorLine(LogColor color, params object[] msgs)
    {
        var msg = DealMsg(true, msgs);
        Debug.LogError(string.Format("<color={0}>{1}</color>", color, msg));
    }

    private static string DealMsg(bool lineBreak, params object[] msgs)
    {
        StringBuilder sb = new StringBuilder();
        var str = lineBreak ? "\n" : "  "; 
        for (int i = 0; i < msgs.Length; i++)
        {
            sb.Append(msgs[i] + str);
        }

        return sb.ToString();
    }
}