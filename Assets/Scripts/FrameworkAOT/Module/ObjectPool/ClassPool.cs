/*--------------------------------------------------------------
 * File: ClassPool.cs
 * Author: Wang ShaoWen
 * Time: 2024/03/14 19:14:37 
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

using System;
using System.Runtime.CompilerServices;

public class ClassPool
{
    // 用数组替代栈，减少方法调用开销，比链表性能也好      在现代CPU架构下，缓存命中率对性能影响巨大；无额外指针开销，内存使用更紧凑；支持批量预分配，减少运行时开销
    // 测试下来数组也栈耗时减少1倍多，主要是因为[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IResetable[] _poolArray;
    private int _index;
    
    //private Stack<IResetable> _pool;
    private Type _type;
    private int _maxCount;

    public ClassPool(Type type, int initCount = 8, int maxCount = -1)
    {
        //_pool = new Stack<IResetable>();
        _maxCount = maxCount;
        _type = type;
        _poolArray = new IResetable[Math.Max(8, initCount)];
        _index = 0;

        if (initCount > 0)
        {
            PreAllocateInternal(initCount, true);
        }
    }

    /// <summary>
    /// 手动预分配对象
    /// </summary>
    /// <param name="count">分配数量</param>
    /// <param name="allowChangeMax">超上限是否修改上限值</param>
    public void PreAllocate(int count, bool allowChangeMax = false)
    {
        if (count <= 0)
        {
            Log.Error(Log.LogColor.Red, "预分配数量必须大于0");
            return;
        }

        PreAllocateInternal(count, allowChangeMax);
    }

//     public T Get<T>() where T : class, IResetable, new()
//     {
//         T ret = null;
//
//         if (_pool.Count > 0)
//         {
//             //TimeProfiler.RecordTimeStart("栈操作");
//             ret = _pool.Pop() as T;
//             //TimeProfiler.RecordTimeStop("栈操作");
// #if INFO_ON
//             info.poolCount--;
// #endif
//         }
//         else
//         {
//             ret = new T();
//         }
// #if INFO_ON
//         info.useCount++;
//         info.activeCount++;
// #endif
//         return ret;
//     }

    public T Get<T>() where T : class, IResetable, new()
    {
        T ret;

        if (_index > 0)
        {
            ret = _poolArray[--_index] as T;
            _poolArray[_index] = null;
        }
        else
        {
            ret = new T();
        }

        return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Recycle<T>(T item) where T : IResetable
    {
        RecycleItem(item);
    }

    public void Clean()
    {
        //_pool.Clear();  
        
        Array.Clear(_poolArray, 0, _index);
        _index = 0;
    }

//     private void RecycleItem(IResetable item)
//     {
//         if (_pool.Contains(item))
//         {
//             Log.Error(Log.LogColor.Red, "对象池中已经包含该对象，请检查回收逻辑");
//             return;
//         }
//         
//         if (_maxCount == -1 || _pool.Count < _maxCount)
//         {
//             item.Reset();
//             _pool.Push(item);
// #if INFO_ON
//             info.poolCount++;
// #endif
//         }
// #if INFO_ON
//         info.activeCount--;
//         info.recycleCount++;
// #endif
//     }

    private void RecycleItem(IResetable item)
    {
#if UNITY_EDITOR  // ide环境开启重复检查
        for (int i = 0; i < _index; i++)
        {
            if (ReferenceEquals(_poolArray[i], item))
            {
                Log.Error(Log.LogColor.Red, "对象池中已经包含该对象，请检查回收逻辑");
                return;
            }
        }
#endif
        if (_maxCount == -1 || _index < _maxCount)
        {
            if (_index >= _poolArray.Length)
            {
                var newSize = _poolArray.Length * 2;
                if (_maxCount != -1 && newSize > _maxCount)
                {
                    newSize = _maxCount;
                }
                Array.Resize(ref _poolArray, newSize);
            }
            
            item.Reset();
            _poolArray[_index++] = item;
        }
    }

    private void PreAllocateInternal(int count, bool allowChangeMax)
    {
        int targetCount = _index + count;
        if (targetCount > _poolArray.Length)
        {
            int newCapacity = Math.Max(_poolArray.Length * 2, targetCount);
            if (_maxCount != -1 && newCapacity > _maxCount)
            {
                if (allowChangeMax)
                {
                    _maxCount = newCapacity;
                }
                else
                {
                    newCapacity = _maxCount;
                }
            }
            Array.Resize(ref _poolArray, newCapacity);
        }
        
        // 创建新对象并加入池中
        for (int i = 0; i < count; i++)
        {
            if (_maxCount != -1 && count >= _maxCount)
            {
                Log.Warning(Log.LogColor.Yellow, $"对象池已达到最大容量 {_maxCount}，停止预分配");
                break;
            }

            var obj = Activator.CreateInstance(_type) as IResetable;
            _poolArray[_index++] = obj;
        }
    }
}