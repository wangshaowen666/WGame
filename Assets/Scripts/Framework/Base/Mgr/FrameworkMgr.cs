/*--------------------------------------------------------------
 * File: FrameworkMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/05/22 11:47:23 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

/// <summary>
/// 框架管理类容器，有状态需清除的加入此容器中，不需要的声明为静态
/// </summary>
public static class FrameworkMgr
{
    public static readonly EventMgr Event = new();
    public static readonly HttpMgr Http = new();
    public static readonly ObjectPoolMgr ObjectPool = new();
    public static readonly ResMgr Res = new();
    public static readonly LuaMgr Lua = new();
    public static readonly SceneMgr Scene = new();
    public static readonly ScreenMgr Screen = new();
    public static readonly TimerMgr Timer = new();
    
    private static readonly ManagerBase[] s_Managers;
    
    static FrameworkMgr()
    {
        // 后声明的先添加，默认后声明的可以依赖到先声明的
        s_Managers = new ManagerBase[]
        {
            Timer,
            Screen,
            Scene,
            Lua,
            Res,
            ObjectPool,
            Http,
            Event,
        };
    }

    public static void OnSceneExit(int sceneTp)
    {
        foreach (var mgr in s_Managers)
        {
            mgr.OnSceneExit(sceneTp);
        }
    }

    public static void OnGameRestart()
    {
        foreach (var mgr in s_Managers)
        {
            mgr.OnGameRestart();
        }
    }
}