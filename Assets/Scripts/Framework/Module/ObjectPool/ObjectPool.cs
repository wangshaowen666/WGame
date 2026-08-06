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
    private readonly Dictionary<string, Stack<T>> _freePool = new();
    private readonly Dictionary<string, HashSet<T>> _activeObjs = new();
    private readonly Dictionary<string, float> _lastPutTime = new();
    private readonly float _autoReleaseTime;
    private readonly int _maxCapacity;

    // 是否已释放，防止定时器竞争
    private bool _isReleased;
    private CancellationTokenSource _cancel;

#if STATS_ON && UNITY_EDITOR
    private readonly Dictionary<string, ObjectPoolStats> _stats = new();
#endif

    public ObjectPool(float autoReleaseTime = 0, int maxCapacity = 100)
    {
        _autoReleaseTime = autoReleaseTime;
        _maxCapacity = maxCapacity;
        if (autoReleaseTime > 0)
            _cancel = CoreMgr.Timer.StartSecondDelay(autoReleaseTime, AutoReleaseObj);
    }

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

    public void PutObj(string key, T obj)
    {
        if (obj == null) return;

        if (!_freePool.TryGetValue(key, out var freeStack))
        {
            freeStack = new Stack<T>();
            _freePool[key] = freeStack;
        }

        bool wasActive = _activeObjs.TryGetValue(key, out var active) && active.Remove(obj);
        if (!wasActive && freeStack.Contains(obj))
        {
            Log.Error($"对象已在对象池中: {obj.name}");
            return;
        }

        if (freeStack.Count >= _maxCapacity)
        {
            ReleaseObj(key, obj);
#if STATS_ON && UNITY_EDITOR
            UpdateStats(key, 3);
#endif
            return;
        }

        freeStack.Push(obj);
        _lastPutTime[key] = Time.time;
#if STATS_ON && UNITY_EDITOR
        UpdateStats(key, wasActive ? 4 : 1);
#endif
    }

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
            foreach (var v in kv.Value)
                ReleaseObj(kv.Key, v);
        }

        _freePool.Clear();
        _activeObjs.Clear();
        _lastPutTime.Clear();
#if STATS_ON && UNITY_EDITOR
        _stats.Clear();
#endif
    }

    // 定时自动释放：按 key 级别判断，超过 _autoReleaseTime 未被使用的 key 全部释放
    private void AutoReleaseObj()
    {
        try
        {
            float currentTime = Time.time;
            List<string> keysToRemove = null;

            foreach (var kv in _freePool)
            {
                if (kv.Value.Count == 0) continue;

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
    // flag: 1-新对象放入, 2-取出, 3-释放, 4-旧对象放回, 5-加载
    private void UpdateStats(string key, int flag)
    {
        if (!_stats.TryGetValue(key, out var stats))
        {
            stats = new ObjectPoolStats();
            _stats[key] = stats;
        }

        switch (flag)
        {
            case 1:
                stats.totalObjects++;
                stats.externalObjects--;
                stats.totalPuts++;
                stats.inactiveObjects++;
                if (stats.activeObjects + stats.inactiveObjects > stats.peakObjects)
                    stats.peakObjects = stats.activeObjects + stats.inactiveObjects;
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
            case 4:
                stats.activeObjects--;
                stats.totalPuts++;
                stats.inactiveObjects++;
                if (stats.activeObjects + stats.inactiveObjects > stats.peakObjects)
                    stats.peakObjects = stats.activeObjects + stats.inactiveObjects;
                break;
            case 5:
                stats.totalLoad++;
                stats.externalObjects++;
                break;
        }
    }

    public Dictionary<string, ObjectPoolStats> GetStats()
    {
        return new Dictionary<string, ObjectPoolStats>(_stats);
    }
#endif
}

#if STATS_ON && UNITY_EDITOR
public class ObjectPoolStats
{
    public int totalObjects;    // 总对象数
    public int activeObjects;   // 活跃对象数
    public int inactiveObjects; // 非活跃对象数
    public int peakObjects;     // 峰值对象数
    public long totalGets;      // 总获取次数
    public long totalPuts;      // 总放回次数
    public long totalRelease;   // 总释放次数
    public int totalLoad;       // 总加载次数（池中无对象时）
    public int externalObjects; // 外部对象数（尚未入池）
}
#endif