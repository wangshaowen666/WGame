/*--------------------------------------------------------------
 * File: Test3.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/19 14:04:13 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

public class PerformanceComparisonTool : MonoBehaviour
{
    [Header("测试配置")]
    [SerializeField] private float testDuration = 30; // 延长测试时长
    [SerializeField] private bool useOptimizedVersion = false;
    [SerializeField] private int objectCount = 10000; // 增加对象数量，更容易触发GC
    [SerializeField] private bool showDebugInfo = true; // 显示调试信息
    private string resultFilePath = "Assets/Scripts/Test/PerformanceComparison/";
    
    [Header("性能指标")]
    private List<float> frameTimes = new List<float>();
    
    // GC统计相关 - 使用Unity Profiler API
    private int gcEventCount;
    private float testStartTime;
    
    // 内存统计相关
    private long allocatedMemoryBefore;
    private long allocatedMemoryAfter;
    private long monoHeapSizeBefore;
    private long monoHeapSizeAfter;
    private long monoUsedSizeBefore;
    private long monoUsedSizeAfter;
    
    void Start()
    {
        // 创建结果目录
        if (!Directory.Exists(resultFilePath))
        {
            Directory.CreateDirectory(resultFilePath);
        }
        
        StartCoroutine(RunPerformanceTest());
    }
    
    IEnumerator RunPerformanceTest()
    {
        Debug.Log("开始性能测试...");
        
        // 记录开始时间
        testStartTime = Time.time;
        
        // 强制进行一次GC，确保初始状态干净
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        
        // 记录内存开始使用情况
        allocatedMemoryBefore = Profiler.GetTotalAllocatedMemoryLong();
        monoHeapSizeBefore = Profiler.GetMonoHeapSizeLong();
        monoUsedSizeBefore = Profiler.GetMonoUsedSizeLong();
        
        // 重置GC事件计数
        gcEventCount = 0;
        
        // 开始采集帧时间
        StartCoroutine(CollectFrameTimes());
        
        // 运行测试场景
        yield return StartCoroutine(RunTestScenario());
        
        // 记录内存结束使用情况
        allocatedMemoryAfter = Profiler.GetTotalAllocatedMemoryLong();
        monoHeapSizeAfter = Profiler.GetMonoHeapSizeLong();
        monoUsedSizeAfter = Profiler.GetMonoUsedSizeLong();
        
        // 分析并保存结果
        AnalyzeAndSaveResults();
    }
    
    IEnumerator CollectFrameTimes()
    {
        frameTimes.Clear();
        
        while (Time.time - testStartTime < testDuration)
        {
            // 记录每一帧的时间
            float frameStart = Time.realtimeSinceStartup;
            
            yield return null; // 等待一帧
            
            float frameEnd = Time.realtimeSinceStartup;
            float frameTime = (frameEnd - frameStart) * 1000f; // 转换为毫秒
            frameTimes.Add(frameTime);
        }
    }
    
    IEnumerator RunTestScenario()
    {
        // 这里模拟你的游戏逻辑
        float elapsedTime = 0f;
        long lastAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
        
        while (elapsedTime < testDuration)
        {
            if (useOptimizedVersion)
            {
                // 运行优化后的逻辑
                Profiler.BeginSample("OptimizedTestScenario");
                RunOptimizedLogic();
                Profiler.EndSample();
            }
            else
            {
                // 运行原始逻辑
                Profiler.BeginSample("OriginalTestScenario");
                RunOriginalLogic();
                Profiler.EndSample();
            }
            
            // 检查内存变化，检测GC事件
            long currentAllocatedMemory = Profiler.GetMonoUsedSizeLong();
            
            // 如果内存减少了1MB以上，可能发生了GC
            if (currentAllocatedMemory < lastAllocatedMemory - 1024 * 1024)
            {
                gcEventCount++;
                if (showDebugInfo)
                {
                    Debug.Log($"检测到GC事件 #{gcEventCount}, 内存从 {lastAllocatedMemory / (1024*1024)}MB 减少到 {currentAllocatedMemory / (1024*1024)}MB");
                }
            }
            
            lastAllocatedMemory = currentAllocatedMemory;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    
    // 这是一个在GC发生后会被调用的方法
    private void OnGCCompleted()
    {
        // 这里可以增加你的GC事件计数和日志
        Debug.Log($"Full GC (Gen 2) 已完成, 总计: ");
    }
    
    void AnalyzeAndSaveResults()
    {
        // 计算平均帧时间
        float totalFrameTime = 0f;
        foreach (var time in frameTimes)
        {
            totalFrameTime += time;
        }
        float averageFrameTime = totalFrameTime / frameTimes.Count;
        
        // 计算GC触发频率
        float testDurationMinutes = testDuration / 60f;
        long totalGC = gcEventCount;
        float gcPerMinute = totalGC / testDurationMinutes;
        
        // 计算内存变化
        long memoryAllocatedDelta = allocatedMemoryAfter - allocatedMemoryBefore;
        long monoHeapSizeDelta = monoHeapSizeAfter - monoHeapSizeBefore;
        long monoUsedSizeDelta = monoUsedSizeAfter - monoUsedSizeBefore;
        
        // 输出调试信息
        if (showDebugInfo)
        {
            Debug.Log($"总GC次数: {totalGC}");
            Debug.Log($"内存分配变化: {memoryAllocatedDelta / (1024*1024)}MB");
            Debug.Log($"Mono堆大小变化: {monoHeapSizeDelta / (1024*1024)}MB");
            Debug.Log($"Mono已用大小变化: {monoUsedSizeDelta / (1024*1024)}MB");
        }
        
        // 创建结果报告
        string result = CreateReport(averageFrameTime, gcPerMinute, totalGC, 
            allocatedMemoryBefore, allocatedMemoryAfter, memoryAllocatedDelta,
            monoHeapSizeBefore, monoHeapSizeAfter, monoHeapSizeDelta,
            monoUsedSizeBefore, monoUsedSizeAfter, monoUsedSizeDelta);
        
        // 保存到文件
        string fileName = useOptimizedVersion ? 
            "Optimized_Performance_Report.txt" : "Original_Performance_Report.txt";
        string fullPath = Path.Combine(resultFilePath, fileName);
        File.WriteAllText(fullPath, result);
        
        Debug.Log($"性能测试完成！结果已保存到: {fullPath}");
        Debug.Log(result);
    }
    
    string CreateReport(float avgFrameTime, float gcPerMinute, long totalGC,
        long allocatedBefore, long allocatedAfter, long allocatedDelta,
        long monoHeapBefore, long monoHeapAfter, long monoHeapDelta,
        long monoUsedBefore, long monoUsedAfter, long monoUsedDelta)
    {
        // 格式化内存大小
        string FormatMemory(long bytes) => (bytes / (1024f * 1024f)).ToString("F2");
        
        return $@"性能测试报告
==============
测试配置:
- 版本: {(useOptimizedVersion ? "优化后" : "优化前")}
- 测试时长: {testDuration}秒 ({testDuration/60f}分钟)
- 对象数量: {objectCount}

性能指标:
==============
1. CPU性能:
- 平均每帧耗时: {avgFrameTime:F2}ms
- 总帧数: {frameTimes.Count}
- 帧率(FPS): {1000f/avgFrameTime:F1}

2. 内存性能:
- GC总触发次数: {totalGC}次
- 每分钟GC次数: {gcPerMinute:F1}次
- 总分配内存: 
  - 开始: {FormatMemory(allocatedBefore)} MB
  - 结束: {FormatMemory(allocatedAfter)} MB
  - 变化: {FormatMemory(allocatedDelta)} MB
- Mono堆大小: 
  - 开始: {FormatMemory(monoHeapBefore)} MB
  - 结束: {FormatMemory(monoHeapAfter)} MB
  - 变化: {FormatMemory(monoHeapDelta)} MB
- Mono已用大小: 
  - 开始: {FormatMemory(monoUsedBefore)} MB
  - 结束: {FormatMemory(monoUsedAfter)} MB
  - 变化: {FormatMemory(monoUsedDelta)} MB

3. 详细帧时间统计:
- 最短帧时间: {GetMinFrameTime():F2}ms
- 最长帧时间: {GetMaxFrameTime():F2}ms
- 第95百分位: {GetPercentile(95):F2}ms

测试时间: {System.DateTime.Now}

备注: 此报告使用内存变化检测GC事件，更准确反映Unity实际GC情况。
";
    }
    
    // 辅助方法
    float GetMinFrameTime()
    {
        float min = float.MaxValue;
        foreach (var time in frameTimes) if (time < min) min = time;
        return min;
    }
    
    float GetMaxFrameTime()
    {
        float max = 0;
        foreach (var time in frameTimes) if (time > max) max = time;
        return max;
    }
    
    float GetPercentile(int percentile)
    {
        var sortedTimes = new List<float>(frameTimes);
        sortedTimes.Sort();
        int index = (int)(sortedTimes.Count * percentile / 100f);
        return sortedTimes[Mathf.Min(index, sortedTimes.Count - 1)];
    }
    
    void RunOriginalLogic()
    {
        // 原始网络同步逻辑
        // 模拟性能较低的实现，频繁创建临时对象，更容易触发GC
        var tempList = new List<Vector3>();
        for (int i = 0; i < objectCount; i++)
        {
            tempList.Add(new Vector3(i, i, i));
        }
        // 清空列表，触发GC
        tempList.Clear();
        // 手动触发GC，确保能检测到
        //System.GC.Collect();
    }
    
    void RunOptimizedLogic()
    {
        // 优化后的网络同步逻辑
        // 模拟性能较高的实现，减少内存分配
        Vector3[] tempArray = new Vector3[objectCount];
        for (int i = 0; i < objectCount; i++)
        {
            tempArray[i] = new Vector3(i, i, i);
        }
        // 数组不需要手动清空，会被自动回收
    }
}