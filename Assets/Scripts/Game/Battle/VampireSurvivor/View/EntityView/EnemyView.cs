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
    private Renderer _renderer;
    private bool _facingRight;
    private int _lastSeenHitFrame; // 上次已处理的受击帧（0=无；与逻辑层 LastHitFrame 比对发现新受击）
    private float _flash;          // 当前闪白量（0~1，逐逻辑帧衰减）

    // 头顶血条数据（2-12，逻辑帧 SetHp 写入，渲染帧由 EnemyHpBarRenderer 读取合并绘制）
    private long _hp;
    private long _maxHp;

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
        _renderer = GetComponentInChildren<Renderer>();
    }

    private void OnEnable()
    {
        ResetInterpolation();
        _facingRight = false;
        _lastSeenHitFrame = 0;
        _flash = 0f;
        _hp = 0;
        _maxHp = 0;
    }

    /// <summary>写入血量（逻辑帧由 VampireView 调用；满血/死亡时血条渲染器不写顶点即隐藏）</summary>
    public void SetHp(long hp, long maxHp)
    {
        _hp = hp;
        _maxHp = maxHp;
    }

    /// <summary>是否需显示血条（临时测试：出场即显示，仅死亡隐藏；验证后恢复"满血不显示"）</summary>
    public bool HasHpBar => _maxHp > 0 && _hp > 0;

    /// <summary>血量比例 0~1（渲染器绘制前景宽度用）</summary>
    public float HpRatio => _maxHp > 0 ? (float)_hp / _maxHp : 0f;

    /// <summary>
    /// 精灵头顶（艺术面上边缘）在 EntityRoot 局部 y 轴上的坐标：
    /// EntityRoot 旋转 90°X 且位于原点，局部 +y 与 world +z 同轴（画面上方），
    /// flat 精灵的 world bounds.max.z 即其局部 y 上边缘（任意轴点/缩放均成立）；渲染器未就绪时按锚点上方兜底
    /// </summary>
    public float HeadTopLocalY => _renderer != null ? _renderer.bounds.max.z : transform.localPosition.y + 0.4f;

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
