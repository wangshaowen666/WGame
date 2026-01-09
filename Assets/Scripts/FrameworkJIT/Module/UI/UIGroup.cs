/*--------------------------------------------------------------
 * File: UIGroup.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/08 10:13:06 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class UIGroup 
{
    private LinkedList<IUIPanel> _uiPanels = new LinkedList<IUIPanel>();
    private Transform _root;

    public UIGroup(Transform root)
    {
        _root = root;
    }

    public void PanelOn(string pnlName, Action<UIPanelBase> callback = null)
    {
        // ResMgr.Instance.LoadPrefab(key, o =>
        // {
        //     var panel = Object.Instantiate(o, _layerMap[layer]);
        //     callback?.Invoke(panel);
        // });
    }

    public void PanelOff(string pnlName, Action<UIPanelBase> callback = null)
    {
        
    }
}