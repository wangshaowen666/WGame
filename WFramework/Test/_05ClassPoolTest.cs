/*--------------------------------------------------------------
 * File: _05ClassPoolTest.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/02 18:53:08 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Pool;

public class _05ClassPoolTest : MonoBehaviour
{
    private float _timer;
    private int _count = 100;

    private ObjectPool<_05Test> _pool;
    
    public static readonly ProfilerMarker Test1 = new ProfilerMarker("Test.Test1");

    void Start()
    {
        int c = 100;
        TimeProfiler.LogTime(() =>
        {
            for (int i = 0; i < c; i++)
            {
                _05Test t = new _05Test();
            }
        });
        
        ClassFactory.PreAllocate<_05Test>(c);
        
        TimeProfiler.LogTime(() =>
        {
            for (int i = 0; i < c; i++)
            {
                _05Test t = ClassFactory.Get<_05Test>();
            }
        });
    }
    
    private void Update()
    {
        // TimeProfiler.RecordTimeStart("对象池取值");
        // for (int i = 0; i < _count; i++)
        // {
        //     _05Test t = ClassPoolFactory.Get<_05Test>();
        //
        // }
        // TimeProfiler.RecordTimeStop("对象池取值");
        //

        using (Test1.Auto())
        {
            TimeProfiler.RecordTimeStart("new创建");
            for (int i = 0; i < _count; i++)
            {
                _05Test t = new _05Test();
        
            }
            TimeProfiler.RecordTimeStop("new创建");
        }
       
        
       
        
        TimeProfiler.RecordTimeStart("对象池取值再放回");
        for (int i = 0; i < _count; i++)
        {
            _05Test t = ClassFactory.Get<_05Test>();
            ClassFactory.Recycle(t);
        }
        TimeProfiler.RecordTimeStop("对象池取值再放回");
        
        
        ClassFactory.PreAllocate<_05Test>(_count);
        TimeProfiler.RecordTimeStart("对象池取值");
        for (int i = 0; i < _count; i++)
        {
            _05Test t = ClassFactory.Get<_05Test>();
        }
        TimeProfiler.RecordTimeStop("对象池取值");
        
        // for (int i = 0; i < _count; i++)
        // {
        //     _05Test t = new _05Test();
        //     ClassPoolFactory.Recycle(t);
        // }
        //
        // TimeProfiler.RecordTimeStart("对象池取值2");
        // for (int i = 0; i < _count; i++)
        // {
        //     _05Test t = ClassPoolFactory.Get<_05Test>();
        // }
        // TimeProfiler.RecordTimeStop("对象池取值2");
        //
        // TimeProfiler.RecordTimeStart("Unity对象池并回收");
        // for (int i = 0; i < _count; i++)
        // {
        //     _05Test t = _pool.Get();
        //     _pool.Release(t);
        // }
        // TimeProfiler.RecordTimeStop("Unity对象池并回收");
        //
        // TimeProfiler.RecordTimeStart("Unity对象池");
        // for (int i = 0; i < _count; i++)
        // {
        //     _05Test t = _pool.Get();
        // }
        // TimeProfiler.RecordTimeStop("Unity对象池");
        
        
        _timer += Time.deltaTime;
        if (_timer > 5f)
        {
            _timer = -10000;
            TimeProfiler.DebugRecordTime();
        }
    }
}

/*
 * 64位系统 类对象头占用16个字节
 * 成员根据类型不同所占字节不同，引用类型都是8个字节（指针）
 * 赋值与否、公有私有不影响内存占用，但是赋值的引用类型除了指针占用还有堆上分配的对象的额外内存
 *
 * new _05Test会有5次gc，自身一次、array一次、List一次，List内部Variable数组一次（实例一个List共两次gc）、VatInt一次；
 * string为字面量，无额外gc开销
 * 只声明不赋值则不会有gc开销，如arg1和v未赋值，gc为3，arg1原本就不会产生gc
 */
public class _05Test : IResetable
{
    private string arg1;
    //private string arg1 = "dfgd";
    private string arg2 = "fs44"; 
    private string arg3 = "fs534";
    private string arg4 = "fsdfg";
    
    private int[] array = new int[100];
    //private List<Variable> v = new List<Variable>(100);
    private List<Variable> v;

    private VarInt v2 = 5;
    
    public void Reset()
    {
        
    }
}