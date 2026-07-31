/*--------------------------------------------------------------
 * File: FrameworkMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/05/22 11:47:23
 *--------------------------------------------------------------
 */

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

    // 下标越小，越先初始化，可被后初始化的依赖，清理时越晚清理
    private static readonly ManagerBase[] s_managers;

    static FrameworkMgr()
    {
        s_managers = new ManagerBase[]
        {
            Event,
            Http,
            ObjectPool,
            Res,
            Lua,
            Scene,
            Screen,
        };
    }

    public static void Init() => ManagerContainerHelper.Init(s_managers);
    public static void OnSceneExit(int sceneTp) => ManagerContainerHelper.OnSceneExit(s_managers, sceneTp);
    public static void OnGameRestart() => ManagerContainerHelper.OnGameRestart(s_managers);
}
