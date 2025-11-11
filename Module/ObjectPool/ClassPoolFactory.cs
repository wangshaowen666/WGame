/*--------------------------------------------------------------
 * File: ClassPoolFactory.cs
 * Author: Wang ShaoWen
 * Time: 2024/03/20 18:23:22 
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public static class ClassPoolFactory
{
    private static Dictionary<Type, ClassPool> poolFactory = new Dictionary<Type, ClassPool>();

    public static IClass Get(Type type)
    {
        if (!poolFactory.TryGetValue(type, out var pool))
        {
            pool = new ClassPool(type);
            poolFactory[type] = pool;
        }
        return pool.Get();
    }

    public static T Get<T>() where T : class, IClass, new()
    {
        var type = typeof(T);
        if (!poolFactory.TryGetValue(type, out var pool))
        {
            pool = new ClassPool(type);
            poolFactory[type] = pool;
        }

        return pool.Get<T>();
    }

    public static void Recycle(IClass item)
    {
        var type = item.GetType();
        if (!poolFactory.TryGetValue(type, out var pool))
        {
            Log.Error(Log.LogColor.Red, "正在回收未经对象池工厂创建的对象：", type.Name);
            return;
        }
        
        pool.Recycle(item);
    }

    public static void Recycle<T>(T item) where T : IClass
    {
        var type = item.GetType();
        if (!poolFactory.TryGetValue(type, out var pool))
        {
            Log.Error(Log.LogColor.Red, "正在回收未经对象池工厂创建的对象：", type.Name);
            return;
        }
        
        pool.Recycle(item);
    }

    public static void Clean(Type type)
    {
        if (!poolFactory.TryGetValue(type, out var pool))
        {
            Log.Error(Log.LogColor.Red, "正在清除工厂中不存在的池子类型：", type.Name);
            return;
        }
        
        pool.Clean();
    }

    public static void CleanAll()
    {
        foreach (var pool in poolFactory.Values)
        {
            pool.Clean();
        }
        
        poolFactory.Clear();
    }

    public static void Check(Type type)
    {
        if (!poolFactory.TryGetValue(type, out var pool))
        {
            Log.Error(Log.LogColor.Red, "正在清除工厂中不存在的池子类型：", type.Name);
            return;
        }
        
        pool.Check();
    }
    
    public static void CheckALl()
    {
        Dictionary<Type, int> poolInfoMap = new Dictionary<Type, int>();
        foreach (var pool in poolFactory.Values)
        {
            pool.Check();
        }
    }
}