/*--------------------------------------------------------------
 * File: EntityViewBase.cs
 * Author: Wsw
 *--------------------------------------------------------------
 */

using UnityEngine;

/// <summary>
/// 实体视图基类（VS 表现层）：逻辑帧位置双缓冲 + 渲染帧插值，
/// 逻辑帧率（20Hz）与渲染帧率（60/120fps）解耦，位置在逻辑帧间平滑过渡。
/// - 逻辑帧：ViewSync 的 Refresh 内调 PushFrame 记录帧间状态（prev/cur）
/// - 渲染帧：LocalDriver.OnRenderFrame 下发 alpha，ApplyInterpolation 写 Transform
/// 子类 OnEnable 调 ResetInterpolation，保证池化复用后首帧 prev=cur，不产生插值跳跃。
/// </summary>
public abstract class EntityViewBase : MonoBehaviour
{
    private const float Height = 0.25f; // 视图高度（相对逻辑 XZ 平面，VS 实体统一）

    private float _prevX = float.NaN; // 上一逻辑帧位置（NaN = 未设置，首帧 prev=cur 不插值）
    private float _prevY;
    private float _curX;
    private float _curY;

    /// <summary>逻辑帧推进：每逻辑帧调用一次（Sync 的 Refresh 内）</summary>
    public void PushFrame(float x, float y)
    {
        if (float.IsNaN(_prevX))
        {
            _prevX = x; // 首帧：prev=cur，避免从原点/旧位置滑入
            _prevY = y;
        }
        else
        {
            _prevX = _curX;
            _prevY = _curY;
        }

        _curX = x;
        _curY = y;
    }

    /// <summary>渲染帧插值：每渲染帧调用一次（alpha 0~1，由驱动器下发）</summary>
    public void ApplyInterpolation(float alpha)
    {
        transform.localPosition = new Vector3(
            Mathf.Lerp(_prevX, _curX, alpha),
            Mathf.Lerp(_prevY, _curY, alpha),
            Height);
    }

    /// <summary>创建/池化复用时重置插值状态（子类 OnEnable 调用）</summary>
    protected void ResetInterpolation()
    {
        _prevX = float.NaN;
    }
}
