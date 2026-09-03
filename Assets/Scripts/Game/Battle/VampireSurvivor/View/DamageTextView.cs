/*--------------------------------------------------------------
 * File: DamageTextView.cs
 * Author: Wsw
 *--------------------------------------------------------------
 */

using TMPro;
using UnityEngine;

/// <summary>
/// 伤害飘字视图（表现层，一次性表现）：命中时显示伤害数字并上浮淡出。
/// 生命周期由 VampireView 管理（EntityPool.Acquire 创建 → 每渲染帧 Tick 动画 → 到期回池）；
/// 禁止自建 Update（项目规范），动画由外部驱动。
/// </summary>
public class DamageTextView : MonoBehaviour
{
    [SerializeField] private TextMeshPro _text; // prefab 上配置（TMP 3D 文本，MiSans SDF）

    private float _riseSpeed = 0.8f;   // 上浮速度（单位/秒，纯视觉）
    private float _life;
    private float _totalLife = 0.6f;   // 存活时长（秒，纯视觉）

    /// <summary>表现实体配置 Id（创建时写入，归还实体池时用作池 key）</summary>
    public int EntityId { get; private set; }

    /// <summary>由 spawn 回调写入实体配置 Id（快照自配置表）</summary>
    public void SetEntityId(int entityId)
    {
        EntityId = entityId;
    }

    /// <summary>落点与内容（创建时设置一次）</summary>
    public void Show(float x, float y, long damage)
    {
        transform.localPosition = new Vector3(x, y, 0.4f); // 高于实体层，避免遮挡
        _life = _totalLife;
        if (_text != null)
        {
            _text.text = damage.ToString();
            _text.color = new Color(1f, 0.9f, 0.2f, 1f);
        }
    }

    /// <summary>每渲染帧推进：上浮 + 淡出（deltaSeconds = 本渲染帧真实秒数）</summary>
    public void Tick(float deltaSeconds)
    {
        var pos = transform.localPosition;
        pos.z += _riseSpeed * deltaSeconds; // 实体根局部 Z = 世界高度方向
        transform.localPosition = pos;

        _life -= deltaSeconds;
        if (_text != null)
        {
            var alpha = Mathf.Clamp01(_life / (_totalLife * 0.5f)); // 后半程淡出
            _text.color = new Color(1f, 0.9f, 0.2f, alpha);
        }
    }
}
