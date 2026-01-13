/*--------------------------------------------------------------
 * File: PanelCtr.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/18 14:42:43 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using cfg;
using UnityEngine;
using Object = UnityEngine.Object;

public class PanelCtr : Singleton<PanelCtr>
{
    private const string UILayerName = "UI";
    private const string UIRootName = "UIRoot";
    
    private readonly Dictionary<DUIGroup, Transform> _layerMap;
    private readonly Dictionary<DUIGroup, UIGroup> _layerGroupMap;
    private readonly int _uiLayerId;
    
    private PanelCtr()
    {
        _layerMap = new Dictionary<DUIGroup, Transform>();
        _layerGroupMap = new Dictionary<DUIGroup, UIGroup>();
        _layerGroupMap = new Dictionary<DUIGroup, UIGroup>();
        
        _uiLayerId = LayerMask.NameToLayer(UILayerName);
        
        CreateUILayers();
    }

    public void PanelOn(DPnlId id, object userData = null)
    {
        var cfg = DataTableCtr.Instance.TbUIPanel[id];
        if (cfg == null)
        {
            throw new GameException("不存在的界面：" + id);
        }

        var g = cfg.Group;
        if (!_layerGroupMap.TryGetValue(g, out var group))
        {
            group = new UIGroup(_layerMap[g]);
            _layerGroupMap.Add(g, group);
        }
        
        group.PanelOn(cfg, userData);
    }

    // public void PanelOff(string key, UILayer layer = UILayer.BOTTOM_2)
    // {
    //     
    // }
    

    private void CreateUILayers()
    {
        var uiRoot = new GameObject { name = UIRootName, layer = _uiLayerId };
        uiRoot.transform.SetParent(ScreenCtr.Instance.UICanvas.transform, false);
        uiRoot.AddComponent<RectTransform>();
			
        foreach (DUIGroup layer in Enum.GetValues(typeof(DUIGroup)))
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