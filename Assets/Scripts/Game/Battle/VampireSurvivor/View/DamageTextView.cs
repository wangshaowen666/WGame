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

    private const float SpawnYOffset = 0.5f; // 生成点偏移（局部 +Y = 世界 +Z = 屏幕上方），从怪物中心移到头顶
    private float _riseSpeed = 0.8f;   // 上浮速度（单位/秒，纯视觉，沿屏幕上方）
    private float _life;
    private float _totalLife = 0.6f;   // 存活时长（秒，纯视觉）
    private Color _color = new(1f, 0.9f, 0.2f, 1f); // 文案颜色（淡出时仅调 alpha）

    /// <summary>表现实体配置 Id（创建时写入，归还实体池时用作池 key）</summary>
    public int EntityId { get; private set; }

    /// <summary>由 spawn 回调写入实体配置 Id（快照自配置表）</summary>
    public void SetEntityId(int entityId)
    {
        EntityId = entityId;
    }

    /// <summary>落点与内容（创建时设置一次）：伤害数字，默认金黄色</summary>
    public void Show(float x, float y, long damage)
    {
        ShowText(x, y, damage.ToString(), new Color(1f, 0.9f, 0.2f, 1f));
    }

    /// <summary>任意文案飘字（宝箱奖励公告 3-6 等）：内容与颜色由表现层组装传入</summary>
    public void ShowText(float x, float y, string content, Color color)
    {
        // EntityRoot 绕 X 旋转 90°（俯视投影），局部 Z 越小 = 世界高度越高：
        // 实体层统一在局部 z=0.25，飘字取 0 = 比实体高 0.25，避免被敌人/掉落物遮挡
        transform.localPosition = new Vector3(x, y + SpawnYOffset, 0f);
        _life = _totalLife;
        _color = color;
        if (_text != null)
        {
            _text.text = content;
            _text.color = color;
        }
    }

    /// <summary>每渲染帧推进：上浮 + 淡出（deltaSeconds = 本渲染帧真实秒数）</summary>
    public void Tick(float deltaSeconds)
    {
        var pos = transform.localPosition;
        // 俯视相机正对世界 ±Y 轴，沿局部 Z（世界高度）移动屏幕上几乎不可见；
        // 屏幕上方 = 世界 +Z = 局部 +Y，上浮沿该轴移动
        pos.y += _riseSpeed * deltaSeconds;
        transform.localPosition = pos;

        _life -= deltaSeconds;
        if (_text != null)
        {
            var alpha = Mathf.Clamp01(_life / (_totalLife * 0.5f)); // 后半程淡出
            _text.color = new Color(_color.r, _color.g, _color.b, alpha);
        }
    }
}
