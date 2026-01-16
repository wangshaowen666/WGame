/*--------------------------------------------------------------
 * File: ClassPoolFactory.cs
 * Author: Wang ShaoWen
 * Time: 2024/03/20 18:23:22 
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

/*
 * 每帧100次获取持续5s 测试发现 耗时比直接new更久(无任何成员的空类) （已优化）
 * 实测当类字段增多，所占内存字节增多，new耗时指数级增长，对象池只会在预创建的时候有一次开销，后续缓存获取耗时变化不大
 *
 * 主要性能问题   
   1. 字典查找开销过大   
   每次Get/Recycle都要进行字典查找，这是最大的性能瓶颈。   
   2. 类型检查过于严格   
   泛型方法中不必要的类型检查增加了开销。   
   3. 堆栈操作可能不如数组高效   
   Stack的Push/Pop操作相比数组索引有额外开销。
   
   自己测试统计发现var type = typeof(T);同样有一定开销
   类型获取 58ms    字典查找85ms    池类型检查 58ms  栈操作 66ms
   
   修改后测试，原版208ms  优化了工厂类型获取和字典查找 取消了池的类型检查  101ms   用数组替换栈后 34ms， 纯new开销 41ms   
   如果去掉[MethodImpl(MethodImplOptions.AggressiveInlining)]  耗时变成69ms，所以数组替换栈的提升没有翻倍这么大
   
   [MethodImpl(MethodImplOptions.AggressiveInlining)] 强制内联建议
   方法内联是JIT（Just-In-Time）编译器的一种优化技术，它将方法调用替换为方法体本身的代码，从而消除方法调用的开销。
   消除的开销：方法调用指令（call指令）参数压栈/出栈 栈帧创建和销毁 寄存器保存和恢复
   性能提升：减少CPU指令数 更好的CPU缓存局部性 减少分支预测失败
   缺点：内联后，每个调用点都会插入完整的代码，可能导致生成的机器代码体积过大；难以断点；调用堆栈不完整；
   
 *
 * 对象池的主要优势是减少GC压力，而不是提升速度。在GC频繁触发时，对象池可以减少GC带来的卡顿。所以，即使对象池的获取速度稍慢，但整体性能可能更稳定。 
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class ClassPool
{
    // 新增池的缓存，使工厂字典查找池的操作由每次都查询改成只在构造时查询一次
    private static class PoolCache<T> where T: class, IResetable, new()
    {
        public static ClassContainer Container;
        static PoolCache()
        {
            var type = typeof(T);
            if (!_poolMap.TryGetValue(type, out Container))
            {
                Container = new ClassContainer(type);
                _poolMap[type] = Container;
            }
        }
    }
    
    private static readonly Dictionary<Type, ClassContainer> _poolMap = new Dictionary<Type, ClassContainer>();
    
    // 新增预分配方法
    public static void PreAllocate(Type type, int count, int maxCount = -1, bool allowChangeMax = true)
    {
        if (count <= 0)
        {
            Log.Error(Log.LogColor.Red, "预分配数量必须大于0");
            return;
        }

        if (!_poolMap.TryGetValue(type, out var pool))
        {
            pool = new ClassContainer(type, count, maxCount);
            _poolMap[type] = pool;
            Log.Info(Log.LogColor.Cyan, $"预分配 {type.Name} 对象池: {count}个对象");
        }
        else
        {
            // 如果池子已存在，进行扩容预分配
            pool.PreAllocate(count, allowChangeMax);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PreAllocate<T>(int count, int maxCount = -1, bool allowChangeMax = true) where T : class, IResetable, new()
    {
        PreAllocate(typeof(T), count, maxCount, allowChangeMax);
    }
    
    /// <summary>
    /// 批量预分配多种类型的对象池
    /// </summary>
    /// <param name="allocationMap"></param>
    public static void PreAllocateBatch(Dictionary<Type, int> allocationMap)
    {
        if (allocationMap == null) return;

        foreach (var kvp in allocationMap)
        {
            PreAllocate(kvp.Key, kvp.Value);
        }
        
        Log.Info(Log.LogColor.Green, $"批量预分配完成，共{allocationMap.Count}种类型");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Get<T>() where T : class, IResetable, new()
    {
        return PoolCache<T>.Container.Get<T>();
    }

    public static void Recycle(IResetable item)
    {
        var type = item.GetType();
        if (!_poolMap.TryGetValue(type, out var pool))
        {
            Log.Error(Log.LogColor.Red, "正在回收未经对象池工厂创建的对象：", type.Name);
            return;
        }
        
        pool.Recycle(item);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Recycle<T>(T item) where T : class, IResetable, new()
    {
        PoolCache<T>.Container.Recycle(item);
    }

    public static void Clean(Type type)
    {
        if (!_poolMap.TryGetValue(type, out var pool))
        {
            Log.Error(Log.LogColor.Red, "正在清除工厂中不存在的池子类型：", type.Name);
            return;
        }
        
        pool.Clean();
    }

    public static void CleanAll()
    {
        foreach (var pool in _poolMap.Values)
        {
            pool.Clean();
        }
        
        _poolMap.Clear();
    }
}