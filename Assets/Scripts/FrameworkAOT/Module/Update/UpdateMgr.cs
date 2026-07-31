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
public static class UpdateMgr
{
    private static readonly List<IUpdateable> s_updateList = new List<IUpdateable>();
    // Update时可能触发注册和移除，放到缓存中，在下一帧的遍历中才生效
    private static readonly List<IUpdateable> s_addCaches = new List<IUpdateable>();
    // 安全性和偷懒性，允许同一个IUpdateable多次调用移除，通过HashSet保证唯一
    private static readonly HashSet<IUpdateable> s_rmvCaches = new HashSet<IUpdateable>();

    private static bool s_addItem;
    private static bool s_rmvItem;
    private static int s_addIndex;
    
#if STATS_ON && UNITY_EDITOR
    private static readonly Unity.Profiling.ProfilerMarker s_updateMarker = new ("WGame.Update");
#endif

    public static void RegisterUpdate(IUpdateable updateable)
    {
        if (s_updateList.Contains(updateable) || s_addCaches.Contains(updateable))
        {
            Log.Error("重复注册 Updateable:", updateable.GetType().Name);
            return;
        }
        
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
            foreach (var u in s_updateList)
            {
                u.MyUpdate(deltaTime, realDeltaTime);
            }
        }
#else
        foreach (var u in s_updateList)
        {
            u.MyUpdate(deltaTime, realDeltaTime);
        }
#endif
    }

    private static void DealCache()
    {
        s_addIndex = 0;
        for (int i = 0; i < s_updateList.Count; i++)
        {
            if (s_addItem && s_addIndex < s_addCaches.Count)
            {
                if (s_updateList[i].Priority < s_addCaches[s_addIndex].Priority)
                {
                    s_updateList.Insert(i, s_addCaches[s_addIndex]);
                    s_addIndex++;
                }
            }
            
            // 如果先减后加，有可能0号元素移除，下标变成-1，加的时候报错
            if (s_rmvItem && s_rmvCaches.Count > 0)
            {
                if (s_rmvCaches.Remove(s_updateList[i]))
                {
                    s_updateList.RemoveAt(i);
                    i--;
                }
            }
        }

        for (; s_addIndex < s_addCaches.Count; s_addIndex++)
        {
            // 解决同一帧，既加又删且优先级比当前s_updateList中都低时，上方遍历s_rmvCaches并未移除
            if (!s_rmvCaches.Remove(s_addCaches[s_addIndex]))
                s_updateList.Add(s_addCaches[s_addIndex]);
        }
        s_addCaches.Clear();

        if (s_rmvCaches.Count > 0)
        {
            Log.Error("尝试移除未注册的Updateable");
            s_rmvCaches.Clear();
        }
    }
}