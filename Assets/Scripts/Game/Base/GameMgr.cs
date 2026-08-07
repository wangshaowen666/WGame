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
    public static readonly DataTableMgr DataTable = new();
    public static readonly UIMgr UI = new();
    public static readonly BattleMgr Battle = new();
    public static readonly EntityMgr Entity = new();

    private static readonly ManagerBase[] s_managers;

    static GameMgr()
    {
        s_managers = new ManagerBase[]
        {
            DataTable,
            UI,
            Battle,
            Entity,
        };
    }

    public static void Init() => ManagerContainerHelper.Init(s_managers);
    public static void OnSceneExit(SceneType sceneTp) => ManagerContainerHelper.OnSceneExit(s_managers, (int)sceneTp);
    public static void OnGameRestart() => ManagerContainerHelper.OnGameRestart(s_managers);
}
