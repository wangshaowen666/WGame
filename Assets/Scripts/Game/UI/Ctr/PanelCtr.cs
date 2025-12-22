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
    private Dictionary<GameLayer, Transform> _layerMap;
    private int _uiLayerId;
    
    private PanelCtr()
    {
        _layerMap = new Dictionary<GameLayer, Transform>();
        _uiLayerId = LayerMask.NameToLayer("UI");
        
        //CreateGameLayers();
    }

    public LoginPanel LoadLoginPanel()
    {
        GameObject loginPrefab = Resources.Load<GameObject>("LoginPanel/LoginRoot");
        if (loginPrefab != null)
        {
            GameObject loginInstance = Object.Instantiate(loginPrefab);
            loginInstance.name = "LoginRoot";
            return loginInstance.GetComponentInChildren<LoginPanel>();
        }
        else
        {
            throw new GameException("Failed to load LoginRoot prefab from Resources/LoginPanel/");
        }
    }

    private void CreateGameLayers()
    {
        var gameRoot = new GameObject{name = "GameRoot"};
        var worldRoot = new GameObject{name = "WorldRoot"};
        var uiRoot = new GameObject{name = "UIRoot"};
        
        worldRoot.transform.SetParent(gameRoot.transform);
        uiRoot.transform.SetParent(gameRoot.transform);
			
        foreach (GameLayer layer in Enum.GetValues(typeof(GameLayer)))
        {
            if (layer < GameLayer.BOTTOM_1)
            {
                _layerMap.Add(layer, CreateLayer(layer, worldRoot.transform));
            }
            else
            {
                _layerMap.Add(layer, CreateLayer(layer, uiRoot.transform));
            }
        }
        
        Object.DontDestroyOnLoad(gameRoot);
    }

    private Transform CreateLayer(GameLayer layer, Transform rootNode)
    {
        var obj = new GameObject{name = layer.ToString()};
        if (layer >= GameLayer.BOTTOM_1)
        {
            obj.layer = _uiLayerId;
        }
        
        var tr = obj.transform;
        tr.parent = rootNode;
        
        return tr;
    }
}