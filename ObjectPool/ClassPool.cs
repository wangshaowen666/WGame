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

public class ClassPool
{
    private Stack<IClass> pool;
    private Type type;
    private int maxCount;

    public ClassPool(Type type, int initCount = 0, int maxCount = -1)
    {
        pool = new Stack<IClass>();
        this.maxCount = maxCount;

        if (initCount > 0)
        {
            for (int i = 0; i < initCount; i++)
            {
                var c = Activator.CreateInstance(type) as IClass;
                pool.Push(c);
            }
        }
    }

    public IClass Get()
    {
        IClass ret = null;
        
        if (pool.Count > 0)
        {
            ret = pool.Pop();
        }
        else
        {
            ret = Activator.CreateInstance(type) as IClass;
        }

        ret?.Init();
        return ret;
    }

    public T Get<T>() where T : class, IClass, new()
    {
        T ret = null;

        if (pool.Count > 0)
        {
            ret = pool.Pop() as T;
        }
        else
        {
            ret = new T();
        }
        
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

    private void RecycleItem(IClass item)
    {
        if (maxCount == -1 || pool.Count < maxCount)
        {
            item.Reset();
            pool.Push(item);
        }
        else
        {
            item.Clean();
        }
    }
}