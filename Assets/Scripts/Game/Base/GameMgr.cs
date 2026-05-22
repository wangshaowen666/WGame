/*--------------------------------------------------------------
 * File: GameMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/05/20 10:17:32 
 *--------------------------------------------------------------
 */

/// <summary>
/// 游戏管理类容器，有状态需清除的加入此容器中，不需要的声明为静态
/// </summary>
public static class GameMgr 
{
    // readonly保证线程安全
    public static readonly DataTableMgr DataTable = new();
    public static readonly UIMgr UI = new();
    public static readonly BattleMgr Battle = new();
    public static readonly EntityMgr Entity = new();
    
    private static readonly ManagerBase[] s_Managers;

    static GameMgr()
    {
        // 后声明的先添加，默认后声明的可以依赖到先声明的
        s_Managers = new ManagerBase[]
        {
            Entity,
            Battle,
            UI,
            DataTable,
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