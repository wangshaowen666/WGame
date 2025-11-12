/*--------------------------------------------------------------
 * File: _02ForAndForeach.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 10:44:42 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 列表遍历方式耗时比较
/// 在数量级不多的情况下，二者性能开销几乎一致
/// </summary>
public class _02ForAndForeach : MonoBehaviour
{
    private List<ForAndForeachTest> _modules;
    private float _time;

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
            TimeProfiler.DebugRecordTime();
        }
    }
}

public class ForAndForeachTest
{
    public void Run()
    {
        
    }
}