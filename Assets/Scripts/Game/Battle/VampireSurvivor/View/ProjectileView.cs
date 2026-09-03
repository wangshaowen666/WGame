/*--------------------------------------------------------------
 * File: ProjectileView.cs
 * Author: Wsw
 *--------------------------------------------------------------
 */

using UnityEngine;

/// <summary>
/// 弹幕视图（表现层，无逻辑驱动）：位置由基类双缓冲插值；创建时朝向飞行方向（仅一次）。
/// </summary>
public class ProjectileView : EntityViewBase
{
    /// <summary>表现实体配置 Id（创建时写入，归还实体池时用作池 key）</summary>
    public int EntityId { get; private set; }

    /// <summary>由 spawn 回调写入实体配置 Id（快照自弹幕实体，防池化复用后被 Reset）</summary>
    public void SetEntityId(int entityId)
    {
        EntityId = entityId;
    }

    private void OnEnable()
    {
        ResetInterpolation();
    }

    /// <summary>创建时朝向飞行方向（绕实体根局部 Z 轴旋转，即俯视水平面内；素材基准朝向约定 +X）。由 SpawnBoltView 在创建回调中调用一次</summary>
    public void SetDirection(float dirX, float dirY)
    {
        var angle = Mathf.Atan2(dirY, dirX) * Mathf.Rad2Deg;
        transform.localEulerAngles = new Vector3(0f, 0f, angle);
    }
}
