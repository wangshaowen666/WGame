/*--------------------------------------------------------------
 * File: EnemyView.cs
 * Author: Wsw
 * Time: 2026/09/01
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

/// <summary>
/// 敌人视图（表现层，无逻辑驱动）：负责位置刷新、朝向翻转与死亡动画播放。
/// 结构同 HeroView：位置/朝向缓存上次值，仅变化才写 Transform；OnEnable（池化复用）时重置缓存。
/// </summary>
public class EnemyView : MonoBehaviour
{
    private const float Height = 0.25f;
    private const string DieClip = "die";

    private static readonly Vector3 FaceLeft = new Vector3(1, 1, 1);
    private static readonly Vector3 FaceRight = new Vector3(-1, 1, 1);

    private Transform _flip;
    private FrameAnimGpuView _anim;
    private bool _facingRight;
    private float _posX = float.NaN;
    private float _posY = float.NaN;

    private void Awake()
    {
        _flip = transform.Find("Flip");
        if (_flip == null)
            Log.Error("EnemyView 未找到子节点 Flip，无法翻转朝向（检查预制体节点名/层级）");
        _anim = GetComponentInChildren<FrameAnimGpuView>();
    }

    private void OnEnable()
    {
        _posX = float.NaN;
        _posY = float.NaN;
        _facingRight = false;
    }

    public void SetPosition(float x, float y)
    {
        if (Mathf.Approximately(x, _posX) && Mathf.Approximately(y, _posY)) return;
        _posX = x;
        _posY = y;
        transform.localPosition = new Vector3(x, y, Height);
    }

    public void SetFlip(float faceX)
    {
        if (_flip == null || faceX == 0f) return;

        bool right = faceX > 0f;
        if (right == _facingRight) return;
        _facingRight = right;
        _flip.localScale = right ? FaceRight : FaceLeft;
    }

    /// <summary>播放死亡动画（视图消亡时触发；播完由 VampireView 计时回池）</summary>
    public void PlayDie()
    {
        if (_anim == null) return;
        _anim.Play(DieClip);
    }
}
