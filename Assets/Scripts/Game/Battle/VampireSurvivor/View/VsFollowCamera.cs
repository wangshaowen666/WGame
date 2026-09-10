/*--------------------------------------------------------------
 * File: VsFollowCamera.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/09/10
 *--------------------------------------------------------------
 */

using Cinemachine;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 吸血鬼幸存者战斗相机（自包含，退出时清理不污染共享相机）：
/// 主相机挂 CinemachineBrain，创建虚拟相机（Transposer 俯视跟随，保持原相机视角）
/// </summary>
public sealed class VsFollowCamera
{
    private CinemachineVirtualCamera _vcam; // 玩家跟随虚拟相机
    private Camera _brainCamera;            // 本战斗为其添加 CinemachineBrain 的相机（退出时移除）

    /// <summary>初始化 Cinemachine 相机跟随：主相机挂 Brain，创建虚拟相机（Transposer 俯视跟随，保持原相机视角）</summary>
    public void Init()
    {
        var cam = GameCamera.MainCamera ?? Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            Log.Warning("[吸血鬼] 未找到相机，跳过 Cinemachine 跟随");
            return;
        }

        if (cam.GetComponent<CinemachineBrain>() == null)
        {
            cam.gameObject.AddComponent<CinemachineBrain>();
            _brainCamera = cam;
        }

        var go = new GameObject("VampireFollowCamera");
        _vcam = go.AddComponent<CinemachineVirtualCamera>();
        _vcam.m_Priority = 20; // 高于默认，进入战斗即被 Brain 采用
        _vcam.m_Lens = LensSettings.FromCamera(cam); // 继承原相机镜头（透视/视野/裁剪面不变）

        // 俯视跟随：Body 用 Transposer（世界空间固定偏移，随英雄平移），不接管旋转
        //（无 Aim 组件 = DoNothing，相机保持下方设定的俯视朝向，不朝向 Follow）
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 对齐原相机俯视朝向
        var body = _vcam.AddCinemachineComponent<CinemachineTransposer>();
        body.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
        body.m_FollowOffset = new Vector3(0f, 10f, 0f); // 正俯视下偏移必须纯垂直（WorldSpace 不随相机旋转换算），英雄居中；带 Z 分量会让英雄偏出屏幕中心
        body.m_XDamping = 2f; // 平滑跟随（相机本地 X/Y = 世界水平面）
        body.m_YDamping = 2f;
        body.m_ZDamping = 0f; // 高度方向无阻尼，保持恒定偏移
    }

    /// <summary>英雄视图就绪后作为跟随目标（仅首个英雄生效，单机单人）</summary>
    public void SetHeroFollow(Transform follow)
    {
        if (_vcam != null && _vcam.Follow == null)
            _vcam.Follow = follow;
    }

    /// <summary>清理：销毁虚拟相机，移除本战斗添加的 Brain（避免污染共享相机）</summary>
    public void Dispose()
    {
        if (_vcam != null)
        {
            Object.Destroy(_vcam.gameObject);
            _vcam = null;
        }
        if (_brainCamera != null)
        {
            var brain = _brainCamera.GetComponent<CinemachineBrain>();
            if (brain != null)
                Object.Destroy(brain);
            _brainCamera = null;
        }
    }
}
