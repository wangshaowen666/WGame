/*--------------------------------------------------------------
 * File: CoreMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/07/31 15:58:44
 *--------------------------------------------------------------
 */

using System;

public static class CoreMgr
{
    public static readonly UpdateMgr Update = new();
    public static readonly TimerMgr Timer = new();
    public static readonly ClassPoolMgr ClassPool = new();
    
    private static readonly ManagerBase[] s_managers;

    static CoreMgr()
    {
        s_managers = new ManagerBase[]
        {
            Update,
            Timer,
            ClassPool,
        };
    }

    public static void Init() => ManagerContainerHelper.Init(s_managers);
    public static void OnSceneExit(int sceneTp) => ManagerContainerHelper.OnSceneExit(s_managers, sceneTp);
    public static void OnGameRestart() => ManagerContainerHelper.OnGameRestart(s_managers);
}
