/*--------------------------------------------------------------
 * File: _10TimeCollect.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/23 19:26:28 
 *--------------------------------------------------------------
 */

using System;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// 耗时不能说明全部，同一个代码块测试三次，打印时间会出现其中一次和另外两次差距挺大的情况，可能是内存扩容集中发生
/// </summary>
public class _10TimeCollect : MonoBehaviour
{
    private Stopwatch _sw;

    public bool isUpdate = false;
    
    void Start()
    {
        _sw = new Stopwatch();
        FrameworkMgr.Timer.StartDelay(5000, () =>
        {
            Log.Info(Log.LogColor.Cyan, "共耗时", _sw.Elapsed.TotalMilliseconds, "毫秒");
            TimeProfiler.LogDurationTime();
        });
    }
    
    
    private void Update()
    {
        if (!isUpdate) return;
        
        // _sw.Start();
        // for (int i = 0; i < 100000; i++)
        // {
        //     int j = i;
        // }
        //
        // _sw.Stop();
        
        
        TimeProfiler.RecordTime("时间统计1", () =>
        {
            for (int i = 0; i < 1000; i++)
            {
                var a = i.ToString();
            }
        });
        
        TimeProfiler.RecordTime("时间统计2", () =>
        {
            for (int i = 0; i < 1000; i++)
            {
                var a = i.ToString();
            }
        });
        
        TimeProfiler.RecordTime("时间统计3", () =>
        {
            for (int i = 0; i < 1000; i++)
            {
                var a = i.ToString();
            }
        });
        
        // TimeProfiler.RecordTimeStart("时间统计");
        // for (int i = 0; i < 100000; i++)
        // {
        //     int j = i;
        // }
        // TimeProfiler.RecordTimeStop("时间统计");
    }
}