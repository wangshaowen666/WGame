/*--------------------------------------------------------------
 * File: ModuleBase.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/11 18:52:59 
 *--------------------------------------------------------------
 */

public interface IUpdateable 
{
    int Priority
    {
        get
        {
            return (int)UpdatePriority.Default;
        }
    }

    /// <summary>
    /// 游戏框架轮询
    /// </summary>
    /// <param name="deltaTime">帧间隔，单位为秒，受timeScale影响</param>
    /// <param name="realDeltaTime">帧间隔，单位为秒，不受timeScale影响</param>
    void Update(float deltaTime, float realDeltaTime);
}

/// <summary>
/// 模块优先级，高的优先轮询
/// 统一配置在这里，直观一点
/// </summary>
public enum UpdatePriority
{
    Test = -10,
    Default = 0,
    Fsm,
}
