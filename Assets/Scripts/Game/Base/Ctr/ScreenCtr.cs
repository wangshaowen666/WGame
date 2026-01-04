/*--------------------------------------------------------------
 * File: ScreenCtr.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/31 16:01:13 
 *--------------------------------------------------------------
 */

using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class ScreenCtr : Singleton<ScreenCtr>
{
    private ScreenCtr()
    {
    }

    public void Init()
    {
        InitUICamera();
        InitCanvas();
    }

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
    
    private void InitUICamera()
    {
        if (UICamera) return;
        
        var obj = new GameObject(GameConfig.UICamera);
        var camera = obj.AddComponent<Camera>();
            
        camera.orthographic = true;
        camera.cullingMask = LayerMask.GetMask("UI");
        camera.clearFlags = CameraClearFlags.Depth;
            
        Object.DontDestroyOnLoad(obj);
        UICamera = camera;
        MainCamera = camera;
    }

    private void InitCanvas()
    {
        if (UICanvas) return;
        
        var obj = new GameObject()
        {
            name = "UICanvas",
            layer = LayerMask.NameToLayer("UI")
        };
        
        var canvas = obj.AddComponent<Canvas>();
        obj.AddComponent<GraphicRaycaster>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = UICamera;
        
        InitCanvasScaler(obj);
        
        Object.DontDestroyOnLoad(obj);
        UICanvas = canvas;
    }

    private void InitCanvasScaler(GameObject obj)
    {
        var model = GameConfig.ScreenSize;
        var scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = model;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = Screen.width * 1.0f / Screen.height > model.x * 1.0f / model.y ? 1 : 0;
    }
}