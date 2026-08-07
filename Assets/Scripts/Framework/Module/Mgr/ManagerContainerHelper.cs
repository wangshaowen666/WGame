/*--------------------------------------------------------------
 * File: ManagerContainerHelper.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/07/31 16:10:00
 *--------------------------------------------------------------
 */

/// <summary>
/// 管理器容器公共逻辑，供 CoreMgr/GameMgr 复用
/// </summary>
public static class ManagerContainerHelper
{
    /// <summary>
    /// 正序初始化
    /// </summary>
    public static void Init(ManagerBase[] managers)
    {
        foreach (var mgr in managers)
            mgr.OnInit();
    }

    /// <summary>
    /// 倒序清理场景资源
    /// </summary>
    public static void OnSceneExit(ManagerBase[] managers, int sceneTp)
    {
        for (int i = managers.Length - 1; i >= 0; i--)
            managers[i].OnSceneExit(sceneTp);
    }

    /// <summary>
    /// 倒序重启
    /// </summary>
    public static void OnGameRestart(ManagerBase[] managers)
    {
        for (int i = managers.Length - 1; i >= 0; i--)
            managers[i].OnGameRestart();
    }
}
