/*--------------------------------------------------------------
 * File: UIGroup.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/08 10:13:06 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using cfg;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class OpenPanelInfo : IResetable
{
    public int SerialId { get; private set; }
    public DUIPanel Cfg { get; private set; }
    public object UserData { get; private set; }

    public static OpenPanelInfo Create(int serialId, DUIPanel cfg, object userData)
    {
        var info = ClassFactory.Get<OpenPanelInfo>();
        info.SerialId = serialId;
        info.Cfg = cfg;
        info.UserData = userData;
        
        return info;
    }
    
    public void Reset()
    {
        SerialId = 0;
        Cfg = null;
        UserData = null;
    }
}

public class UIGroup
{
    private LinkedList<UIPanelBase> _uiPanels;
    private List<DPnlId> _loadingIds;
    private readonly Transform _root;
    
    private ObjectPool<UIPanelBase> _uiPanelPool;

    public UIGroup(Transform root)
    {
        _root = root;
        _uiPanels = new LinkedList<UIPanelBase>();
        _loadingIds = new List<DPnlId>();

        _uiPanelPool = new ObjectPool<UIPanelBase>();
    }

    public void PanelOn(DUIPanel cfg, object userData = null)
    {
        if (!cfg.AllowMult)
        {
            if (_loadingIds.Contains(cfg.Id))
                return;
            
            if (HasPanel(cfg.Id))
                return;
        }
        
        // 对象池只负责存，不负责创建
        UIPanelBase panel = _uiPanelPool.GetObj(cfg.Name);
        if (panel == null)
        {
            var info = OpenPanelInfo.Create(0, cfg, userData);
            ResMgr.Instance.LoadAsync<GameObject>(cfg.Name, OnLoadFinish, info);
        }
        else
        {
            InnerPanelOn(panel, 0, userData);
        }
    }

    public bool HasPanel(DPnlId id)
    {
        var panel = _uiPanels.First;
        while (panel != null)
        {
            if (panel.Value.PnlId == id)
                return true;
            
            panel = panel.Next;
        }
        
        return false;
    }

    public void PanelOff(string pnlName, Action<UIPanelBase> callback = null)
    {
        
    }

    private void OnLoadFinish(GameObject obj, object userData)
    {
        OpenPanelInfo info = userData as OpenPanelInfo;
        if (info == null)
        {
            Log.Error("出错了");
            return;
        }
        
        var prefab =Object.Instantiate(obj, _root);
        var panel = prefab.GetComponent<UIPanelBase>();
        if (panel == null)
            throw new GameException("预制体上缺少UIPanelBase脚本");
        
        panel.OnInit(info.Cfg);
        InnerPanelOn(panel, info.SerialId, userData);
        ClassFactory.Recycle(info);
    }
    
    private void InnerPanelOn(UIPanelBase panel, int serialId, object userData)
    {
        panel.OnOpen(serialId, userData);
        _uiPanels.AddFirst(panel);
        Refresh();
    }

    private void Refresh()
    {
        var current = _uiPanels.First;
        // 整组隐藏功能
        //bool hide = m_Hide;
        bool hide = false;
        bool cover = false;
        while (current != null && current.Value != null)
        {
            if (hide)
            {
                if (current.Value.State != PanelState.Hide)
                {
                    current.Value.OnHide();
                }
            }
            else
            {
                if (!cover)
                {
                    if (current.Value.State != PanelState.Active)
                    {
                        current.Value.OnResume();
                    }

                    if (current.Value.IsHideCovered)
                    {
                        hide = true;
                    }
                    else
                    {
                        cover = true;
                    }
                }
                else
                {
                    if (current.Value.State != PanelState.Cover)
                    {
                        current.Value.OnCover();
                    }
                }
            }

            current = current.Next;
        }
    }
}