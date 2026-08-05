using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

// 提供给Mgr存储的类型，字典没法直接存范型类
public interface IObjectPool
{
    void Release();
#if STATS_ON && UNITY_EDITOR
    Dictionary<string, ObjectPoolStats> GetStats();
#endif
}

// 对象从Addressable加载为异步，池只提供缓存功能，不负责创建；预热功能也由外部自行按需实现
// 外部创建的对象，需要清理时，只需执行将其放回池中，池负责清理
public class ObjectPool<T> : IObjectPool where T : Object
{
    // 空闲对象栈
    private readonly Dictionary<string, Stack<T>> _freePool = new();

    // 活跃对象集合
    private readonly Dictionary<string, HashSet<T>> _activeObjs = new();

    // 按 key 记录最后放入时间，自动释放按 key 级别判断
    private readonly Dictionary<string, float> _lastPutTime = new();
    
    // 自动释放时间，单位秒
    private readonly float _autoReleaseTime;
    // 每个 key 的空闲对象最大容量
    private readonly int _maxCapacity;
    // 是否已释放，防止定时器竞争
    private bool _isReleased;
    private CancellationTokenSource _cancel;
    public ObjectPool(float autoReleaseTime = 0, int maxCapacity = 100)
    {
        _autoReleaseTime = autoReleaseTime;
        _maxCapacity = maxCapacity;
        if (autoReleaseTime > 0)
        {
            _cancel = CoreMgr.Timer.StartSecondDelay(autoReleaseTime, AutoReleaseObj);
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
        
        if (_freePool.TryGetValue(key, out var stack) && stack.Count > 0)
        {
            obj = stack.Pop();

            if (!_activeObjs.TryGetValue(key, out var active))
            {
                active = new HashSet<T>();
                _activeObjs[key] = active;
            }
            active.Add(obj);
        }

        if (obj == null)
        {
#if STATS_ON && UNITY_EDITOR
            UpdateStats(key, 5);
#endif
            return null;
        }
        
#if STATS_ON && UNITY_EDITOR
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
        if (obj == null) return;

        // 获取或创建空闲栈
        if (!_freePool.TryGetValue(key, out var freeStack))
        {
            freeStack = new Stack<T>();
            _freePool[key] = freeStack;
        }

        // 从活跃集合移除
        bool wasActive = _activeObjs.TryGetValue(key, out var active) && active.Remove(obj);

        if (!wasActive)
        {
            // 不在活跃集合中，可能是新对象或重复放入
            if (freeStack.Contains(obj))
            {
                Log.Error($"对象已在对象池中: {obj.name}");
                return;
            }
        }

        _lastPutTime[key] = Time.time;
        // 容量检查
        if (freeStack.Count >= _maxCapacity)
        {
            ReleaseObj(key, obj);
#if STATS_ON && UNITY_EDITOR
            UpdateStats(key, 3);
#endif
            return;
        }

        freeStack.Push(obj);
        
#if STATS_ON && UNITY_EDITOR
        UpdateStats(key, wasActive ? 4 : 1);
#endif
    }

    private void AutoReleaseObj()
    {
        try
        {
            float currentTime = Time.time;
            List<string> keysToRemove = null;
            
            foreach (var kv in _freePool)
            {
                if (kv.Value.Count == 0) continue;

                // 按 key 级别判断：该 key 最后一次放入距今超过 autoReleaseTime
                if (currentTime - _lastPutTime.GetValueOrDefault(kv.Key) > _autoReleaseTime)
                {
                    while (kv.Value.Count > 0)
                    {
                        ReleaseObj(kv.Key, kv.Value.Pop());
#if STATS_ON && UNITY_EDITOR
                        UpdateStats(kv.Key, 3);
#endif
                    }

                    keysToRemove ??= new List<string>();
                    keysToRemove.Add(kv.Key);
                }
            }
            
            
            if (keysToRemove != null)
            {
                foreach (var key in keysToRemove)
                {
                    _freePool.Remove(key);
                    _lastPutTime.Remove(key);
                }
            }
        }
        finally
        {
            if (!_isReleased)
                _cancel = CoreMgr.Timer.StartSecondDelay(_autoReleaseTime, AutoReleaseObj);
        }
    }
    
    /// <summary>
    /// 释放对象池
    /// </summary>
    public void Release()
    {
        _isReleased = true;
        if (_cancel != null)
        {
            CoreMgr.Timer.Stop(_cancel);
            _cancel = null;
        }
        
        foreach (var kv in _freePool)
        {
            while (kv.Value.Count > 0)
                ReleaseObj(kv.Key, kv.Value.Pop());
        }
        
        foreach (var kv in _activeObjs)
        {
            if (kv.Value.Count > 0)
            {
                foreach (var v in kv.Value)
                {
                    ReleaseObj(kv.Key, v);
                }
            }
        }

        _freePool.Clear();
        _activeObjs.Clear();
        _lastPutTime.Clear();

        // 这里不应该清空，而是统计释放
#if STATS_ON && UNITY_EDITOR
        _stats.Clear();
#endif    
    }

    private void ReleaseObj(string key, T obj)
    {
        if (obj == null) return;

        if (obj is MonoBehaviour mono)
            Object.Destroy(mono.gameObject);
        else
            Object.Destroy(obj);

        CoreMgr.Res.Unload(key);
    }
    
#if STATS_ON && UNITY_EDITOR
    // 统计信息
    private Dictionary<string, ObjectPoolStats> _stats = new Dictionary<string, ObjectPoolStats>();
    
    /// <summary>
    /// 更新统计信息
    /// </summary>
    /// <param name="key">对象key</param>
    /// <param name="flag">操作标识。1存新的 2取 3释放 4存旧的 5取但没取到</param>
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

#if STATS_ON && UNITY_EDITOR
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
