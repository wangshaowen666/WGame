/*--------------------------------------------------------------
 * File: TimeProfiler.cs
 * Author: Wang ShaoWen
 * Time: 2024/03/20 14:19:09 
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

using System;
using System.Diagnostics;
using UnityEngine;

public static class TimeProfiler 
{
    public static void LogTime(Action cb)
    {
        var sw = new Stopwatch();
        sw.Start();
        cb?.Invoke();
        sw.Stop();
        Log.Info(Log.LogColor.Cyan, "共耗时", sw.Elapsed.TotalMilliseconds, "毫秒");
    }
}