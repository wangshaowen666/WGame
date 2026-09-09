/*--------------------------------------------------------------
 * File: SafeArea.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/07
 *--------------------------------------------------------------
 */

using UnityEngine;

/// <summary>
/// 安全区适配：把所在节点的矩形收进 Screen.safeArea。
/// 挂在面板 prefab 的功能层子节点上（背景层不要挂），子 UI 相对它做锚点布局。
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeArea : MonoBehaviour
{
    private RectTransform _rt;
    private Rect _lastSafeArea;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // 仅编辑器需要：拖窗口、Device Simulator 切机型时 safeArea 会变
        if (Screen.safeArea != _lastSafeArea)
        {
            Apply();
        }
    }
#endif

    private void Apply()
    {
        var sa = Screen.safeArea;
        if (sa.width <= 0 || sa.height <= 0)
            return;

        _lastSafeArea = sa;

        var screenW = Screen.width;
        var screenH = Screen.height;

        _rt.anchorMin = new Vector2(sa.x / screenW, sa.y / screenH);
        _rt.anchorMax = new Vector2((sa.x + sa.width) / screenW, (sa.y + sa.height) / screenH);
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }
}
