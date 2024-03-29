/*--------------------------------------------------------------
 * File: ClassPool.cs
 * Author: Wang ShaoWen
 * Time: 2024/03/14 19:14:37 
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct PoolInfo
{
    /// <summary>
    /// 使用过的数量
    /// </summary>
    public int useCount;
    /// <summary>
    /// 回收的数量
    /// </summary>
    public int recycleCount;
    /// <summary>
    /// 当前活跃的数量
    /// </summary>
    public int activeCount;
    /// <summary>
    /// 对象池中的数量
    /// </summary>
    public int poolCount;

    public void Clean()
    {
        useCount = 0;
        recycleCount = 0;
        activeCount = 0;
        poolCount = 0;
    }
}

public class ClassPool
{
    private Stack<IClass> pool;
    private Type type;
    private int maxCount;
#if UNITY_EDITOR
    // 只在编辑器下统计池子对象使用情况
    private PoolInfo info = new PoolInfo();
#endif

    public ClassPool(Type type, int initCount = 0, int maxCount = -1)
    {
        pool = new Stack<IClass>();
        this.maxCount = maxCount;
        this.type = type;

        if (initCount > 0)
        {
            for (int i = 0; i < initCount; i++)
            {
                var c = Activator.CreateInstance(type) as IClass;
                pool.Push(c);
            }
#if UNITY_EDITOR
            info.poolCount += initCount;
#endif
        }
    }

    public IClass Get()
    {
        IClass ret = null;
        
        if (pool.Count > 0)
        {
            ret = pool.Pop();
#if UNITY_EDITOR
            info.poolCount--;
#endif
        }
        else
        {
            ret = Activator.CreateInstance(type) as IClass;
        }
#if UNITY_EDITOR
        info.useCount++;
        info.activeCount++;
#endif
        ret?.Init();
        return ret;
    }

    public T Get<T>() where T : class, IClass, new()
    {
        if (typeof(T) != type)
        {
            Log.Error(Log.LogColor.Red, "类型不符合，期望类型：", typeof(T).Name, "池中类型：", type.Name);
            return null;
        }
        
        T ret = null;

        if (pool.Count > 0)
        {
            ret = pool.Pop() as T;
#if UNITY_EDITOR
            info.poolCount--;
#endif
        }
        else
        {
            ret = new T();
        }
#if UNITY_EDITOR
        info.useCount++;
        info.activeCount++;
#endif
        ret?.Init();
        return ret;
    }
    
    public void Recycle(IClass item)
    {
        RecycleItem(item);
    }

    public void Recycle<T>(T item) where T : IClass
    {
        RecycleItem(item);
    }

    public void Clean()
    {
        // 当池中对象没有被外部引用时，pool.clear后对象会等待gc回收，无需遍历重置
        // 但是当栈中对象还被外部持有时，则无法释放
        // C#可以处理循环引用的情况，如果两个类互相引用且都没有被外部引用，也会被gc回收
        // 目前结论时。不需要遍历执行clean
        pool.Clear();  
#if UNITY_EDITOR
        info.Clean();
#endif
    }

    public void Check()
    {
#if UNITY_EDITOR
        Log.InfoLine(Log.LogColor.Orange, type.Name + "对象池使用情况:",
            "使用次数：" + info.useCount,
            "回收次数：" + info.recycleCount,
            "当前活跃对象：" + info.activeCount,
            "池中对象数：" + info.poolCount);
#endif
    }

    private void RecycleItem(IClass item)
    {
        if (item.GetType() != type)
        {
            Log.Error(Log.LogColor.Red, "类型不符合，放入类型：", item.GetType().Name, "池中类型：", type.Name);
            return;
        }

        if (pool.Contains(item))
        {
            Log.Error(Log.LogColor.Red, "对象池中已经包含该对象，请检查回收逻辑");
            return;
        }
        
        if (maxCount == -1 || pool.Count < maxCount)
        {
            item.Reset();
            pool.Push(item);
#if UNITY_EDITOR
            info.poolCount++;
#endif
        }
        else
        {
            item.Clean();
        }
#if UNITY_EDITOR
        info.activeCount--;
        info.recycleCount++;
#endif
    }
}