using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

// 提供给Mgr存储的类型，字典没法直接存范型类
public interface IObjectPool
{
    void Release();
#if STATS_ON
    Dictionary<string, ObjectPoolStats> GetStats();
#endif
}

// 对象从Addressable加载为异步，池只提供缓存功能，不负责创建；预热功能也由外部自行按需实现
public class ObjectPool<T> : IObjectPool where T : Object
{
    // 对象池项，包含对象和相关元数据
    private class ObjectItem : IResetable
    {
        public Object Target;       // 实际对象
        public float LastUseTime;   // 最后使用时间
        public bool IsUsing;        // 是否正在使用中

        public static ObjectItem Create(Object target)
        {
            var obj = ClassPool.Get<ObjectItem>();
            obj.Target = target;
            obj.LastUseTime = Time.time;
            obj.IsUsing = false;
        
            return obj;
        }

        public void Reset()
        {
            Target = null;
        }
    }
    
    // 存储对象池数据
    private readonly Dictionary<string, List<ObjectItem>> _pool;
    
    // 自动释放时间，单位秒
    private float _autoReleaseTime;
    private CancellationTokenSource _cancel;
    public ObjectPool(float autoReleaseTime = 0)
    {
        _pool = new Dictionary<string, List<ObjectItem>>();
        _autoReleaseTime = autoReleaseTime;
        if (autoReleaseTime > 0)
        {
            _cancel = Timer.StartSecondDelay(autoReleaseTime, AutoReleaseObj);
        }
    }
    
    /// <summary>
    /// 获取对象
    /// </summary>
    /// <param name="key">对象标识符</param>
    /// <returns>获取的对象</returns>
    public T GetObj(string key)
    {
        T obj = null;
        
        // 尝试从对象池中获取
        if (_pool.TryGetValue(key, out List<ObjectItem> objs))
        {
            for (int i = objs.Count - 1; i >= 0; i--)
            {
                if (!objs[i].IsUsing)
                {
                    obj = objs[i].Target as T;
                    objs[i].IsUsing = true;
                    objs[i].LastUseTime = Time.time;
                    break;
                }
            }
        }

        if (obj == null)
        {
#if STATS_ON
            UpdateStats(key, 5);
#endif
            return null;
        }
        
#if STATS_ON
        UpdateStats(key, 2);
#endif
        return obj;
    }
    
    /// <summary>
    /// 放回对象
    /// </summary>
    /// <param name="key">对象标识符</param>
    /// <param name="obj">要放回的对象</param>
    public void PutObj(string key, T obj)
    {
        // 检查对象池是否存在
        if (!_pool.TryGetValue(key, out List<ObjectItem> objs))
        {
            objs = new List<ObjectItem>();
            _pool.Add(key, objs);
        }

        // 检查对象是否已经在对象池中
        ObjectItem poolItem = null;
        foreach (var item in objs)
        {
            if (item.Target == obj)
            {
                if (!item.IsUsing)
                {
                    Log.Error(typeof(T), $"对象已经在对象池中: {obj.name}");
                    return;
                }
                
                poolItem = item;
                break;
            }
        }
        
        // 如果对象不在对象池中，创建新的池项
        if (poolItem == null)
        {
            poolItem = ObjectItem.Create(obj);
            objs.Add(poolItem);
#if STATS_ON
            UpdateStats(key, 1);
#endif
        }
        else
        {
            poolItem.IsUsing = false;
            poolItem.LastUseTime = Time.time;
#if STATS_ON
            UpdateStats(key, 4);
#endif
        }
    }

    private void AutoReleaseObj(int state)
    {
        // 正常计时完成
        if (state == 1)
        {
            float currentTime = Time.time;
            // 创建键的副本，避免在遍历过程中修改字典导致异常
            var keysToCheck = new List<string>(_pool.Keys);
            foreach (var key in keysToCheck)
            {
                if (!_pool.TryGetValue(key, out var items))
                {
                    continue;
                }
                
                // 检查是否需要释放长时间未使用的对象
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    if (!items[i].IsUsing && currentTime - items[i].LastUseTime > _autoReleaseTime)
                    {
                        ReleaseObj(items[i].Target);
                        ClassPool.Recycle(items[i]);
                        items.RemoveAt(i);
                        
#if STATS_ON
                        UpdateStats(key, 3);
#endif
                    }
                }

                if (items.Count == 0)
                {
                    _pool.Remove(key);
                }
            }
            
            _cancel = Timer.StartSecondDelay(_autoReleaseTime, AutoReleaseObj);
        }
    }
    
    /// <summary>
    /// 释放对象池
    /// </summary>
    public void Release()
    {
        _cancel?.Cancel();
        // 销毁所有对象
        foreach (var kv in _pool)
        {
            foreach (var item in kv.Value)
            {
                ReleaseObj(item.Target);
                ClassPool.Recycle(item);
            }
        }
        
        _pool.Clear();
#if STATS_ON
        _stats.Clear();
#endif    
    }

    private void ReleaseObj(Object obj)
    {
        if (obj is MonoBehaviour monoBehaviour)
        {
            Object.Destroy(monoBehaviour.gameObject);
        }
        else
        {
            Object.Destroy(obj);
        }
    }
    
#if STATS_ON
    // 统计信息
    private Dictionary<string, ObjectPoolStats> _stats = new Dictionary<string, ObjectPoolStats>();
    
    /// <summary>
    /// 更新统计信息
    /// </summary>
    /// <param name="key">对象key</param>
    /// <param name="flag">操作标识。1存新的 2取 3释放 4存已有的 5取但没取到</param>
    private void UpdateStats(string key, int flag)
    {
        if (!_stats.TryGetValue(key, out ObjectPoolStats stats))
        {
            stats = new ObjectPoolStats();
            _stats[key] = stats;
        }

        switch (flag)
        {
            case 1:
            case 4:
                if (flag == 4)
                {
                    stats.activeObjects--;
                }
                else
                {
                    stats.totalObjects++;
                    stats.externalObjects--;
                }
                
                stats.totalPuts++;
                stats.inactiveObjects++;
                if (stats.activeObjects + stats.inactiveObjects > stats.peakObjects)
                {
                    stats.peakObjects = stats.activeObjects + stats.inactiveObjects;
                }
                break;
            
            case 2:
                stats.totalGets++;
                stats.activeObjects++;
                stats.inactiveObjects--;
                break;
            
            case 3:
                stats.totalObjects--;
                stats.inactiveObjects--;
                stats.totalRelease++;
                break;
            
            case 5:
                stats.totalLoad++;
                stats.externalObjects++;
                break;
        }
    }
    
    /// <summary>
    /// 获取对象池统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public Dictionary<string, ObjectPoolStats> GetStats()
    {
        return new Dictionary<string, ObjectPoolStats>(_stats);
    }
#endif
}

#if STATS_ON
/// <summary>
/// 对象池统计信息
/// </summary>
public class ObjectPoolStats
{
    public int totalObjects;    // 总对象数
    public int activeObjects;   // 活跃对象数
    public int inactiveObjects; // 非活跃对象数
    public int peakObjects;     // 峰值对象数
    public long totalGets;      // 总获取次数
    public long totalPuts;      // 总放回次数
    public long totalRelease;   // 总释放次数
    public int totalLoad;       // 总加载次数（对象池取不到）
    public int externalObjects; // 外部对象（还未进到池子的新对象）
}
#endif
