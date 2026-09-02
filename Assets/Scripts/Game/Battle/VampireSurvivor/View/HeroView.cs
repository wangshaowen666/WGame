/*--------------------------------------------------------------
 * File: PlayerView.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/01 15:45:54 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

/// <summary>
/// 英雄视图（表现层，无逻辑驱动）：负责位置刷新与朝向翻转。
/// - 位置/朝向缓存上次值，仅变化时才写 Transform（避免无谓 dirty）；
///   OnEnable（池化复用）时重置缓存，保证复用后首次刷新强制写入。
/// - Flip 子节点在 Awake 缓存一次（避免每帧查找）。
/// </summary>
public class HeroView : MonoBehaviour
{
    private const float Height = 0.25f; // 视图高度（相对逻辑 XZ 平面的 Y）

    private static readonly Vector3 FaceLeft = new Vector3(1, 1, 1);
    private static readonly Vector3 FaceRight = new Vector3(-1, 1, 1);

    private Transform _flip;
    private bool _facingRight;
    private float _posX = float.NaN; // 上次位置（NaN = 未设置，首次/复用后强制写入）
    private float _posY = float.NaN;

    private void Awake()
    {
        _flip = transform.Find("Flip");
        if (_flip == null)
            Log.Error("HeroView 未找到子节点 Flip，无法翻转朝向（检查预制体节点名/层级）");
    }

    /// <summary>池化复用时重置缓存，保证复用后首次刷新强制写入（回池再取不会误判"没变"）</summary>
    private void OnEnable()
    {
        _posX = float.NaN;
        _posY = float.NaN;
        _facingRight = false;
    }

    /// <summary>刷新位置：位置未变则跳过写入</summary>
    public void SetPosition(float x, float y)
    {
        if (Mathf.Approximately(x, _posX) && Mathf.Approximately(y, _posY)) return;
        
        _posX = x;
        _posY = y;
        transform.localPosition = new Vector3(x, y, Height);
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
