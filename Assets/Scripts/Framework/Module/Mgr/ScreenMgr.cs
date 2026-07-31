/*--------------------------------------------------------------
 * File: ScreenCtr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/31 16:01:13 
 *--------------------------------------------------------------
 */

using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class ScreenMgr : ManagerBase
{
    public Canvas UICanvas { get; private set; }
    public Camera UICamera { get; private set; }
    public Camera MainCamera { get; private set; }

    public void SetMainCamera(string cameraName)
    {
        var camera = Camera.allCameras.FirstOrDefault(cam => string.Equals(cam.name, cameraName));
        if (!camera)
        {
            Log.Error("想要设置的相机不存在：", cameraName);
            return;
        }

        var cameraData = camera.GetUniversalAdditionalCameraData();
        if (cameraData.renderType != CameraRenderType.Base)
        {
            cameraData.renderType = CameraRenderType.Base;
        }
            
        var uiCameraData = UICamera.GetUniversalAdditionalCameraData();
        if (uiCameraData.renderType != CameraRenderType.Overlay)
        {
            uiCameraData.renderType = CameraRenderType.Overlay;
        }
            
        if (!cameraData.cameraStack.Contains(UICamera))
        {
            cameraData.cameraStack.Add(UICamera);
        }
        
        MainCamera = camera;
    }
}