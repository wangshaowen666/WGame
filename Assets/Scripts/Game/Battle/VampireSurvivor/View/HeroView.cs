/*--------------------------------------------------------------
 * File: HeroView.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/01 15:45:54
 *--------------------------------------------------------------
 */

using UnityEngine;

/// <summary>
/// 英雄视图（表现层，无逻辑驱动）：负责朝向翻转与死亡表现；位置由基类双缓冲插值。
/// - 朝向缓存上次值，仅变化时才写 Transform；OnEnable（池化复用）时重置。
/// - Flip 子节点在 Awake 缓存一次（避免每帧查找）。
/// </summary>
public class HeroView : EntityViewBase
{
    private static readonly Vector3 FaceLeft = new Vector3(1, 1, 1);
    private static readonly Vector3 FaceRight = new Vector3(-1, 1, 1);

    private Transform _flip;
    private bool _facingRight;

    /// <summary>表现实体配置 Id（创建时写入，归还实体池时用作池 key）</summary>
    public int EntityId { get; private set; }

    /// <summary>由 spawn 回调写入实体配置 Id（快照自逻辑实体）</summary>
    public void SetEntityId(int entityId)
    {
        EntityId = entityId;
    }

    private void Awake()
    {
        _flip = transform.Find("Flip");
        if (_flip == null)
            Log.Error("HeroView 未找到子节点 Flip，无法翻转朝向（检查预制体节点名/层级）");
    }

    private void OnEnable()
    {
        ResetInterpolation();
        _facingRight = false;
    }

    /// <summary>按水平朝向设置翻转：faceX==0（纯上下移动）保持上次朝向；朝向未变不重设 scale</summary>
    public void SetFlip(float faceX)
    {
        if (_flip == null || faceX == 0f) return;

        bool right = faceX > 0f;
        if (right == _facingRight) return;
        _facingRight = right;
        _flip.localScale = right ? FaceRight : FaceLeft;
    }
}
