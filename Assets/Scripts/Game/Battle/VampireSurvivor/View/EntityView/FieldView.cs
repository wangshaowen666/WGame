/*--------------------------------------------------------------
 * File: FieldView.cs
 * Author: Wsw
 * Time: 2026/09/12
 *--------------------------------------------------------------
 */

using UnityEngine;

/// <summary>
/// 区域效果视图（表现层，无逻辑驱动）：位置由基类双缓冲插值（圆心随宿主）。
/// Orbit 环绕体：视图本体 = 单个环绕单元的表现（逻辑层每单元一个独立判定，表现层按
/// Field.Amount 均分圆周角布置 N 个视觉单元——MVP 单体表现即整组（父节点旋转即可），
/// prefab 约定：本组件所在节点为环绕单元（或光环圈），由 SetOrbit 决定是否参与公转自转。
/// </summary>
public class FieldView : EntityViewBase
{
    /// <summary>表现实体配置 Id（创建时写入，归还实体池时用作池 key）</summary>
    public int EntityId { get; private set; }

    private bool _orbit;
    private int _amount = 1;
    private int _angleSteps; // 逻辑层 Angle 累计步数（× 每步弧度 = 当前相位）

    private static readonly float s_rotStepRad = 0.075f; // 与 VsFieldSystem.s_rotStep 一致（表现换算用）

    private void OnEnable()
    {
        ResetInterpolation();
        _orbit = false;
        _amount = 1;
        _angleSteps = 0;
    }

    public void SetEntityId(int entityId)
    {
        EntityId = entityId;
    }

    /// <summary>区域初始化（创建回调调用一次）：isOrbit=环绕体（挂视觉单元旋转），amount=单元数</summary>
    public void SetField(bool isOrbit, int amount)
    {
        _orbit = isOrbit;
        _amount = amount < 1 ? 1 : amount;
        _angleSteps = 0;
        ApplyRotation();
    }

    /// <summary>每逻辑帧刷新（对账）：记录逻辑相位步数</summary>
    public void PushAngle(int angleSteps)
    {
        _angleSteps = angleSteps;
    }

    /// <summary>渲染帧：位置插值（随宿主移动）+ Orbit 按当前相位旋转视觉单元组</summary>
    public void ApplyField(float alpha)
    {
        ApplyInterpolation(alpha); // 位置跟随（圆心随宿主，双缓冲插值与其他实体一致）

        if (_orbit)
            ApplyRotation();
    }

    private void ApplyRotation()
    {
        // 相位 = 步数 × 每步弧度 → 度；MVP 表现：单节点绕圆心公转（本节点挂在圆心下的旋转容器），多单元由 prefab 内布点均分圆周
        var deg = _angleSteps * s_rotStepRad * Mathf.Rad2Deg;
        transform.localEulerAngles = new Vector3(0f, 0f, deg);
    }
}
