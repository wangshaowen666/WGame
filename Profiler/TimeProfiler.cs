/*--------------------------------------------------------------
 * File: TimeProfiler.cs
 * Author: Wang ShaoWen
 * Time: 2024/03/20 14:19:09 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

public static class TimeProfiler 
{
    private static readonly Dictionary<string, RTime> recordTDic = new Dictionary<string, RTime>(); //记录调试时间字典
    
    public static void LogTime(Action cb)
    {
        var sw = new Stopwatch();
        sw.Start();
        cb?.Invoke();
        sw.Stop();
        Log.Info(Log.LogColor.Cyan, "共耗时", sw.Elapsed.TotalMilliseconds, "毫秒");
    }
    
    /// <summary>
    /// 开始记录
    /// </summary>
    /// <param name="key">关键帧名字</param>
    public static void RecordTimeStart(string key)
    {
        var startTime = DateTime.UtcNow.Ticks;
        if (recordTDic.TryGetValue(key, out var r))
        {
            r.startTime = startTime;
        }
        else
        {
            RTime t = new RTime
            {
                startTime = startTime
            };
            recordTDic[key] = t;
        }
    }

    /// <summary>
    /// 结束记录
    /// </summary>
    /// <param name="key">关键帧名字</param>
    public static void RecordTimeStop(string key)
    {
        var stopTime = DateTime.UtcNow.Ticks;
        if (recordTDic.TryGetValue(key, out var r))
        {
            stopTime -= r.startTime;
            r.count++;
            r.allTime += stopTime;
        }
    }

    /// <summary>
    /// 输出统计
    /// </summary>
    /// <param name="isClear">是否清除缓存</param>
    /// <param name="isMs">true为毫秒，否则为秒</param>
    /// <param name="isSaveLocal">是否存储到本地</param>
    public static void DebugRecordTime(bool isClear = true, bool isMs = true, bool isSaveLocal = false)
    {
        string ret = "";
        foreach (var kv in recordTDic)
        {
            var mul = isMs ? 10000 : 10000000;
            var str = $"记录耗时: {kv.Key}  {kv.Value.allTime / mul}  执行次数: {kv.Value.count}";
            Log.Info(Log.LogColor.Cyan, str);

            if (isSaveLocal)
            {
                ret += str + "\n";
            }
        }

        if (isClear)
        {
            recordTDic.Clear();
        }

        if (isSaveLocal)
        {
            FileUtil.WriteText(ret, "耗时统计.txt");
        }
    }
}

public class RTime
{
    public long allTime;
    public long startTime;
    public int count;
}