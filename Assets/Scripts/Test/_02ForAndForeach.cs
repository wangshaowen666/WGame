/*--------------------------------------------------------------
 * File: _02ForAndForeach.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 10:44:42 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// 列表遍历方式耗时比较
/// 在数量级不多的情况下，二者性能开销几乎一致
/// </summary>
public class _02ForAndForeach : MonoBehaviour
{
    private List<ForAndForeachTest> _modules;
    private float _time;


    private List<ForAndForeachTest> _modules2;
    
    public static readonly ProfilerMarker Test1 = new ProfilerMarker("Test.Test1");
    public static readonly ProfilerMarker Test2 = new ProfilerMarker("Test.Test2");

    private void Start()
    {
        _modules = new List<ForAndForeachTest>();
        _modules.Add(new ForAndForeachTest());
        _modules.Add(new ForAndForeachTest());
        _modules.Add(new ForAndForeachTest());
        _modules.Add(new ForAndForeachTest());
        _modules.Add(new ForAndForeachTest());
        _modules.Add(new ForAndForeachTest());
        _modules.Add(new ForAndForeachTest());
        _modules.Add(new ForAndForeachTest());
        _modules.Add(new ForAndForeachTest());
        _modules.Add(new ForAndForeachTest());

        _modules2 = new List<ForAndForeachTest>();
        for (int i = 0; i < 100; i++)
        {
            _modules2.Add(new ForAndForeachTest());
        }
    }

    private void Update()
    {
        _time += Time.deltaTime;

        TimeProfiler.RecordTimeStart("For计时");
        for (int i = 0; i < _modules.Count; i++)
        {
            _modules[i].Run();
        }
        TimeProfiler.RecordTimeStop("For计时");
        
        TimeProfiler.RecordTimeStart("Foreach计时");
        foreach (var module in _modules)
        {
            module.Run();
        }
        TimeProfiler.RecordTimeStop("Foreach计时");
        
        if (_time > 60)
        {
            _time = 0;
            TimeProfiler.LogDurationTime();
        }
        
        GCTest();
    }

    // 经测试，foreach不会生成迭代器引起gc
    private void GCTest()
    {
        using (Test1.Auto())
        {
            for (int i = 0; i < _modules2.Count; i++)
            {
                _modules2[i].Run();
            }
        }

        using (Test2.Auto())
        {
            foreach (var m in _modules2)
            {
                m.Run();
            }
        }
    }
}

public class ForAndForeachTest
{
    public void Run()
    {
        
    }
}