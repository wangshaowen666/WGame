using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class ObjectPool<T> where T : Object
{
    private readonly Dictionary<string, List<T>> _pool;
    
    public ObjectPool()
    {
        _pool = new Dictionary<string, List<T>>();
    }

    public T GetObj(string key)
    {
        if (!_pool.TryGetValue(key, out List<T> objs))
        {
            return null;
        }

        var count = objs.Count;
        if (count == 0)
            return null;

        var ret = objs[count - 1];
        objs.RemoveAt(count - 1);
        return ret;
    }

    public void PutObj(string key, T obj)
    {
        if (!_pool.TryGetValue(key, out List<T> objs))
        {
            objs = new List<T>();
            _pool.Add(key, objs);
        }
        
        objs.Add(obj);
    }
}
