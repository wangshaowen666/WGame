/*--------------------------------------------------------------
 * File: PanelCtr.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/18 14:42:43 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class PanelCtr : Singleton<PanelCtr>
{
    private Dictionary<UILayer, Transform> _layerMap;
    private int _uiLayerId;
    
    private PanelCtr()
    {
        _layerMap = new Dictionary<UILayer, Transform>();
        _uiLayerId = LayerMask.NameToLayer("UI");
        
        CreateUILayers();
    }

    public void PanelOn(string key, UILayer layer = UILayer.BOTTOM_2, Action<GameObject> callback = null)
    {
        ResMgr.Instance.LoadPrefab(key, o =>
        {
            var panel = Object.Instantiate(o, _layerMap[layer]);
            callback?.Invoke(panel);
        });
    }

    private void CreateUILayers()
    {
        var uiRoot = new GameObject { name = "UIRoot", layer = _uiLayerId };
        uiRoot.transform.SetParent(ScreenCtr.Instance.UICanvas.transform, false);
        uiRoot.AddComponent<RectTransform>();
			
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            var obj = new GameObject
            {
                name = layer.ToString(),
                layer = _uiLayerId
            };

            obj.AddComponent<RectTransform>();
            var tr = obj.transform;
            tr.SetParent(uiRoot.transform, false);
            _layerMap.Add(layer, tr);
        }
    } 
}