/*--------------------------------------------------------------
 * File: EffectView.cs
 * Author: Wsw
 *--------------------------------------------------------------
 */

using UnityEngine;

/// <summary>
/// 特效视图（表现层，无逻辑驱动）：命中特效/冒血等一次性表现。
/// 生命周期由外部管理（EntityPool.Acquire 创建 → 播放动画 → Timer 计时回池），
/// 默认动画由 FrameAnimGpuView 在 OnEnable 自动重播（激活即播）。
/// </summary>
public class EffectView : MonoBehaviour
{
    private const float Height = 0.25f; // 视图高度（相对逻辑 XZ 平面，与实体同级）

    /// <summary>表现实体配置 Id（创建时写入，归还实体池时用作池 key）</summary>
    public int EntityId { get; private set; }

    /// <summary>由 spawn 回调写入实体配置 Id（快照自配置表）</summary>
    public void SetEntityId(int entityId)
    {
        EntityId = entityId;
    }

    /// <summary>特效落点（创建时设置一次，播放期间位置固定）</summary>
    public void SetPosition(float x, float y)
    {
        transform.localPosition = new Vector3(x, y, Height);
    }
}
