/*--------------------------------------------------------------
 * File: DropView.cs
 * Author: Wsw
 *--------------------------------------------------------------
 */

/// <summary>
/// 掉落物视图（表现层，无逻辑驱动）：经验宝石，落点静止；位置由基类双缓冲插值。
/// </summary>
public class DropView : EntityViewBase
{
    /// <summary>表现实体配置 Id（创建时写入，归还实体池时用作池 key）</summary>
    public int EntityId { get; private set; }

    /// <summary>由 spawn 回调写入实体配置 Id（快照自逻辑实体，防池化复用后被 Reset）</summary>
    public void SetEntityId(int entityId)
    {
        EntityId = entityId;
    }

    private void OnEnable()
    {
        ResetInterpolation();
    }
}
