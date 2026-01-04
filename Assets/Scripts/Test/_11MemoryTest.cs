/*--------------------------------------------------------------
 * File: _11MemoryTest.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/30 13:38:50 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class _11MemoryTest : MonoBehaviour
{
    private List<byte[]> _managedLst = new List<byte[]>();
    
    private List<Texture2D> _nativeLst = new List<Texture2D>();

    [ContextMenu("Managed Memory")]
    void AllocateManged()
    {
        ManagedMemoryTest(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                byte[] arr = new byte[1024 * 1024];
                _managedLst.Add(arr);
            }
        }, null);
    }

    [ContextMenu("Native Memory")]
    void AllocateNative()
    {
        NativeMemoryTest(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                Texture2D t2d = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                _nativeLst.Add(t2d);
            }
        }, () =>
        {
            foreach (var tex in _nativeLst)
            {
                 DestroyImmediate(tex);   
            }
            _nativeLst.Clear();
        });
    }

    private void ManagedMemoryTest(Action allocCb, Action clearCb)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Log.Info($"Mono 内存分配前使用： {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1024f / 1024f:F2}MB");
        allocCb?.Invoke();
        Log.Info($"Mono 内存分配后使用： {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1024f / 1024f:F2}MB");
        
        clearCb?.Invoke();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Log.Info($"Mono 内存清理后使用： {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1024f / 1024f:F2}MB");
    }

    private void NativeMemoryTest(Action allocCb, Action clearCb)
    {
        // 本地内存无GC流程
        Log.Info($"本地内存分配前使用： {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f:F2}MB");
        allocCb?.Invoke();
        Log.Info($"本地内存分配后使用： {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f:F2}MB");
        
        clearCb?.Invoke();
        Log.Info($"本地内存清理后使用： {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f:F2}MB");
    }
}