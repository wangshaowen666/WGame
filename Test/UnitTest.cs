/*--------------------------------------------------------------
 * File: UnitTest.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/11 14:37:57 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class UnitTest : MonoBehaviour
{
    private TestA a;
    private TestB b;
    void Start()
    {
        var a = CoroutineRunner.Instance.Run(T());
        CoroutineRunner.Instance.Stop(a);
    }

    private IEnumerator T()
    {
        yield return new WaitForSeconds(1);
    }

    void Update()
    {
        if (Time.time - lastCheckTime > checkInterval)
        {
            CheckGC();
            lastCheckTime = Time.time;
        }
    }
    
    
    [Header("监控配置")]
    [SerializeField] private float checkInterval = 1f;
    [SerializeField] private long gcThresholdBytes = 1024 * 1024; // 1MB
    [SerializeField] private bool enableAlert = true;
    
    private long lastTotalMemory;
    private int gcCountPerSecond;
    private float lastCheckTime;
    
    void CheckGC()
    {
        // 累积分配的托管内存总量，包含已回收的
        long currentMemory = Profiler.GetTotalAllocatedMemoryLong();
        // 当前实际占用内存大小
        //long currentMemory = Profiler.GetTotalReservedMemoryLong();
        
        long allocatedSinceLastCheck = currentMemory - lastTotalMemory;
        
        if (allocatedSinceLastCheck > gcThresholdBytes && enableAlert)
        {
            Debug.LogWarning($"[GCMonitor] 检测到大量内存分配: {allocatedSinceLastCheck / 1024f / 1024f:F2}MB");
            
            // 发送到监控后端
            SendGCAlert(allocatedSinceLastCheck);
        }
        
        lastTotalMemory = currentMemory;
        
        // 统计GC次数
        // （需要使用Profiler API或自定义计数）
    }
    
    void SendGCAlert(long bytes)
    {
        // 集成监控系统（如Sentry、Firebase）
        // Analytics.LogEvent("gc_alert", new Dictionary<string, object>
        // {
        //     { "allocated_mb", bytes / 1024f / 1024f },
        //     { "scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name }
        // });
    }
}

public class TestA : IUpdateable
{
    private float time;

    public void Update(float logicTime, float realTime)
    {
        time += logicTime;
        if (time > 1)
        {
            time = 0;
            Log.Info("TestA执行Update");
        }
    }
}

public class TestB : IUpdateable
{
    private float time;

    public void Update(float logicTime, float realTime)
    {
        time += logicTime;
        if (time > 1)
        {
            time = 0;
            Log.Info("TestB执行Update");
        }
    }
}