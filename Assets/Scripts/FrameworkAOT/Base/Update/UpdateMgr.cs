/*--------------------------------------------------------------
 * File: GameFrameworkEntry.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/11 11:29:18 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;

/// <summary>
/// 规避GF框架很多空的Update调用，采用按需注册的形式
/// </summary>
public static class UpdateMgr
{
    private static readonly List<IUpdateable> s_updateMaps = new List<IUpdateable>();
    // Update时可能触发注册和移除，放到缓存中，在下一帧的遍历中才生效
    private static readonly List<IUpdateable> s_addCaches = new List<IUpdateable>();
    // 安全性和偷懒性，允许同一个IUpdateable多次调用移除，通过HashSet保证唯一
    private static readonly HashSet<IUpdateable> s_rmvCaches = new HashSet<IUpdateable>();

    private static bool s_addItem;
    private static bool s_rmvItem;
    
#if STATS_ON && UNITY_EDITOR
    private static readonly Unity.Profiling.ProfilerMarker s_updateMarker = new ("WGame.Update");
#endif

    public static void RegisterUpdate(IUpdateable updateable)
    {
        s_addItem = true;
        for (int i = 0; i < s_addCaches.Count; i++)
        {
            if (updateable.Priority > s_addCaches[i].Priority)
            {
                s_addCaches.Insert(i, updateable);
                return;
            }
        }
        
        s_addCaches.Add(updateable);
    }

    public static void UnRegisterUpdate(IUpdateable updateable)
    {
        s_rmvItem = true;
        s_rmvCaches.Add(updateable);
    }
    
    public static void Update(float deltaTime, float realDeltaTime)
    {
        // 有些在update之前执行的添加或销毁，当帧就生效，比如OnTriggerEnter碰撞后即移除
        if (s_addItem || s_rmvItem)
        {
            DealCache();
            s_addItem = false;
            s_rmvItem = false;
        }

#if STATS_ON && UNITY_EDITOR
        using (s_updateMarker.Auto())
        {
            foreach (var u in s_updateMaps)
            {
                u.MyUpdate(deltaTime, realDeltaTime);
            }
        }
#else
        foreach (var u in s_updateMaps)
        {
            u.MyUpdate(deltaTime, realDeltaTime);
        }
#endif
    }

    private static void DealCache()
    {
        for (int i = 0; i < s_updateMaps.Count; i++)
        {
            if (s_addItem && s_addCaches.Count > 0)
            {
                if (s_updateMaps[i].Priority < s_addCaches[0].Priority)
                {
                    s_updateMaps.Insert(i, s_addCaches[0]);
                    s_addCaches.RemoveAt(0);
                }
            }
            
            // 先减后加，有可能0号元素移除，下标变成-1，加的时候报错
            if (s_rmvItem && s_rmvCaches.Count > 0)
            {
                if (s_rmvCaches.Remove(s_updateMaps[i]))
                {
                    s_updateMaps.RemoveAt(i);
                    i--;
                }
            }
        }

        if (s_addCaches.Count > 0)
        {
            foreach (var updateable in s_addCaches)
            {
                s_updateMaps.Add(updateable);
            }
            s_addCaches.Clear();
        }

        if (s_rmvCaches.Count > 0)
        {
            Log.Error("尝试移除未注册的Updateable");
            s_rmvCaches.Clear();
        }
    }
}