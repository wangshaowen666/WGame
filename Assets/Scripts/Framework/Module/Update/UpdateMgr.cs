/*--------------------------------------------------------------
 * File: GameFrameworkEntry.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/11 11:29:18 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;

/// <summary>
/// 规避GF框架很多空的Update调用，采用按需注册的形式
/// </summary>
public class UpdateMgr : ManagerBase
{
    private readonly List<IUpdateable> _updateList = new List<IUpdateable>();
    // Update时可能触发注册和移除，放到缓存中，在下一帧的遍历中才生效
    private readonly List<IUpdateable> _addCaches = new List<IUpdateable>();
    // 安全性和偷懒性，允许同一个IUpdateable多次调用移除，通过HashSet保证唯一
    private readonly HashSet<IUpdateable> _rmvCaches = new HashSet<IUpdateable>();

    private bool _isAdd;
    private bool _isRmv;
    private int _addIndex;
    
#if STATS_ON && UNITY_EDITOR
    private readonly Unity.Profiling.ProfilerMarker _updateMarker = new ("WGame.Update");
#endif

    public void RegisterUpdate(IUpdateable updateable)
    {
        if (_updateList.Contains(updateable) || _addCaches.Contains(updateable))
        {
            Log.Error("重复注册 Updateable:", updateable.GetType().Name);
            return;
        }
        
        _isAdd = true;
        for (int i = 0; i < _addCaches.Count; i++)
        {
            if (updateable.Priority > _addCaches[i].Priority)
            {
                _addCaches.Insert(i, updateable);
                return;
            }
        }
        
        _addCaches.Add(updateable);
    }

    public void UnRegisterUpdate(IUpdateable updateable)
    {
        _isRmv = true;
        _rmvCaches.Add(updateable);
    }
    
    public void MyUpdate(float deltaTime, float realDeltaTime)
    {
        // 有些在update之前执行的添加或销毁，当帧就生效，比如OnTriggerEnter碰撞后即移除
        if (_isAdd || _isRmv)
        {
            DealCache();
            _isAdd = false;
            _isRmv = false;
        }

#if STATS_ON && UNITY_EDITOR
        using (_updateMarker.Auto())
        {
            foreach (var u in _updateList)
            {
                u.MyUpdate(deltaTime, realDeltaTime);
            }
        }
#else
        foreach (var u in _updateList)
        {
            u.MyUpdate(deltaTime, realDeltaTime);
        }
#endif
    }

    private void DealCache()
    {
        _addIndex = 0;
        for (int i = 0; i < _updateList.Count; i++)
        {
            if (_isAdd && _addIndex < _addCaches.Count)
            {
                if (_updateList[i].Priority < _addCaches[_addIndex].Priority)
                {
                    _updateList.Insert(i, _addCaches[_addIndex]);
                    _addIndex++;
                }
            }
            
            // 如果先减后加，有可能0号元素移除，下标变成-1，加的时候报错
            if (_isRmv && _rmvCaches.Count > 0)
            {
                if (_rmvCaches.Remove(_updateList[i]))
                {
                    _updateList.RemoveAt(i);
                    i--;
                }
            }
        }

        for (; _addIndex < _addCaches.Count; _addIndex++)
        {
            // 解决同一帧，既加又删且优先级比当前s_updateList中都低时，上方遍历s_rmvCaches并未移除
            if (!_rmvCaches.Remove(_addCaches[_addIndex]))
                _updateList.Add(_addCaches[_addIndex]);
        }
        _addCaches.Clear();

        if (_rmvCaches.Count > 0)
        {
            Log.Error("尝试移除未注册的Updateable");
            _rmvCaches.Clear();
        }
    }
}