/*--------------------------------------------------------------
 * File: EnemyView.cs
 * Author: Wsw
 * Time: 2026/09/01
 *--------------------------------------------------------------
 */

using UnityEngine;

/// <summary>
/// 敌人视图（表现层，无逻辑驱动）：负责朝向翻转、死亡动画播放与受击闪白；位置由基类双缓冲插值。
/// OnEnable（池化复用）时重置插值/朝向/受击状态。
/// </summary>
public class EnemyView : EntityViewBase
{
    private const string DieClip = "die";
    private const float FlashFull = 1f;        // 受击闪白初值
    private const float FlashDecay = 0.1f;     // 每逻辑帧衰减（20Hz 下约 0.5s 回落，纯视觉节奏）

    private static readonly Vector3 FaceLeft = new Vector3(1, 1, 1);
    private static readonly Vector3 FaceRight = new Vector3(-1, 1, 1);

    private Transform _flip;
    private FrameAnimGpuView _anim;
    private bool _facingRight;
    private int _lastSeenHitFrame; // 上次已处理的受击帧（0=无；与逻辑层 LastHitFrame 比对发现新受击）
    private float _flash;          // 当前闪白量（0~1，逐逻辑帧衰减）

    /// <summary>表现实体配置 Id（创建时写入，归还实体池时用作池 key）</summary>
    public int EntityId { get; private set; }

    /// <summary>由 spawn 回调写入实体配置 Id（快照自逻辑实体，防池化复用后被 Reset）</summary>
    public void SetEntityId(int entityId)
    {
        EntityId = entityId;
    }

    private void Awake()
    {
        _flip = transform.Find("Flip");
        if (_flip == null)
            Log.Error("EnemyView 未找到子节点 Flip，无法翻转朝向（检查预制体节点名/层级）");
        _anim = GetComponentInChildren<FrameAnimGpuView>();
    }

    private void OnEnable()
    {
        ResetInterpolation();
        _facingRight = false;
        _lastSeenHitFrame = 0;
        _flash = 0f;
    }

    public void SetFlip(float faceX)
    {
        if (_flip == null || faceX == 0f) return;

        bool right = faceX > 0f;
        if (right == _facingRight) return;
        _facingRight = right;
        _flip.localScale = right ? FaceRight : FaceLeft;
    }

    /// <summary>受击感知：帧号变化即新受击（闪白置满），返回是否为新受击（供外部触发命中特效）</summary>
    public bool OnHitFrame(int hitFrame)
    {
        if (hitFrame == 0 || hitFrame == _lastSeenHitFrame)
            return false;

        _lastSeenHitFrame = hitFrame;
        _flash = FlashFull;
        return true;
    }

    /// <summary>每逻辑帧推进闪白衰减（SetFlash 写材质克隆，归零后停写）</summary>
    public void TickFlash()
    {
        if (_flash <= 0f) return;

        _flash = Mathf.Max(0f, _flash - FlashDecay);
        if (_anim != null)
            _anim.SetFlash(_flash);
    }

    /// <summary>播放死亡动画（视图消亡时触发；播完由 VampireView 计时回池）</summary>
    public void PlayDie()
    {
        if (_anim == null) return;
        _anim.Play(DieClip);
    }
}
