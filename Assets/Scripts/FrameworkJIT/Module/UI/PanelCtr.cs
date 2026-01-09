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
    private readonly Dictionary<UILayer, Transform> _layerMap;
    private readonly Dictionary<UILayer, UIGroup> _layerGroupMap;
    private readonly int _uiLayerId;
    
    private PanelCtr()
    {
        _layerMap = new Dictionary<UILayer, Transform>();
        _layerGroupMap = new Dictionary<UILayer, UIGroup>();
        _uiLayerId = LayerMask.NameToLayer("UI");
    }

    public void PanelOn(int id, Action<UIPanelBase> callback = null)
    {
        //var cfg =
        
        // if (!_layerMap.TryGetValue(layer, out var group))
        // {
        //     group = new UIGroup(_layerMap[layer]);
        //     _layerGroupMap.Add(layer, group);
        // }
        //
        // group.PanelOn(key, callback);
    }

    public void PanelOff(string key, UILayer layer = UILayer.BOTTOM_2)
    {
        
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