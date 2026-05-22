/*--------------------------------------------------------------
 * File: PanelCtr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/18 14:42:43 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using cfg;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIMgr : ManagerBase
{
    private const string UILayerName = "UI";
    private const string UIRootName = "UIRoot";
    
    private readonly Dictionary<DUIGroup, UIGroup> _layerGroupMap = new();
    // 正在加载中的界面，如果还没加载完就被关闭了，value会被置为0
    private readonly Dictionary<uint, DPnlId> _loadingPanelIdMap = new();
    private readonly int _uiLayerId = LayerMask.NameToLayer(UILayerName);
    
    private Transform _uiRoot;
    private ObjectPool<UIPanelBase> _uiPanelPool = FrameworkMgr.ObjectPool.RegisterPool<UIPanelBase>(5);

    public void CreateUIRoot()
    {
        var uiRoot = new GameObject { name = UIRootName, layer = _uiLayerId };
        uiRoot.transform.SetParent(FrameworkMgr.Screen.UICanvas.transform, false);
        uiRoot.AddComponent<RectTransform>();
        
        _uiRoot = uiRoot.transform;
    }

    public void PanelOn(DPnlId id, object userData = null)
    {
        var cfg = GameMgr.DataTable.TbUIPanel[id];
        if (cfg == null)
        {
            throw new GameException("不存在的界面：" + id);
        }

        var g = cfg.Group;
        if (!_layerGroupMap.TryGetValue(g, out var group))
        {
            group = CreateUIGroup(g);
        }
        
        if (!cfg.AllowMult)
        {
            if (_loadingPanelIdMap.ContainsValue(cfg.Id))
                return;

            var pnl = HasPanel(cfg.Id);
            if (pnl != null && pnl.State != PanelState.Active)
            {
                group.MoveToTop(pnl, userData);
                return;
            }
        }

        uint loadingId = AutoID.GetID();
        UIPanelBase panel = _uiPanelPool.GetObj(id.ToString());
        if (panel == null)
        {
            _loadingPanelIdMap.Add(loadingId, id);
            var info = LoadPanelArg.Create(loadingId, cfg, group, userData);
            FrameworkMgr.Res.LoadAsync<GameObject>(cfg.Name, OnLoadFinish, info);
        }
        else
        {
            group.AddPanel(panel, userData);
        }
    }
    
    public void PanelOff(DPnlId id)
    {
        var panel = HasPanel(id);
        if (panel != null)
        {
            PanelOff(panel);
            return;
        }

        foreach (var kv in _loadingPanelIdMap)
        {
            if (kv.Value == id)
            {
                _loadingPanelIdMap[kv.Key] = 0;
                break;
            }
        }

        Log.Error("正在关闭不存在的界面：", id);
    }

    public void PanelOff(UIPanelBase panel)
    {
        var cfg = GameMgr.DataTable.TbUIPanel[panel.PnlId];
        _uiPanelPool.PutObj(panel.PnlId.ToString(), panel);
        
        var group = _layerGroupMap[cfg.Group];
        group.RemovePanel(panel);
    }

    public void PanelOffAll()
    {
        foreach (var kv in _loadingPanelIdMap)
        {
            _loadingPanelIdMap[kv.Key] = 0;
        }

        foreach (var kv in _layerGroupMap)
        {
            kv.Value.RemoveAll();
        }
    }

    public UIPanelBase HasPanel(DPnlId id)
    {
        var cfg = GameMgr.DataTable.TbUIPanel[id];
        var group = _layerGroupMap[cfg.Group];
        return group.HasPanel(id);
    }
    
    private void OnLoadFinish(GameObject obj, object userData)
    {
        LoadPanelArg arg = userData as LoadPanelArg;
        if (arg == null)
            throw new GameException("打开界面参数无效");

        _loadingPanelIdMap.Remove(arg.LoadingId, out var pnlId);
        // 加载的过程中被关闭了
        if (pnlId == 0)
        {
            // todo 这里加载到内存，但没有实例化，卸载的时候要注意
            ClassPool.Recycle(arg);
            return;
        }

        var prefab =Object.Instantiate(obj, arg.Group.Trans);
        var panel = prefab.GetComponent<UIPanelBase>();
        if (panel == null)
            throw new GameException("预制体上缺少UIPanelBase脚本");
        
        panel.OnInit(arg.Cfg);
        arg.Group.AddPanel(panel, arg.UserData);
        ClassPool.Recycle(arg);
    }

    private UIGroup CreateUIGroup(DUIGroup groupId)
    {
        var obj = new GameObject
        {
            name = groupId.ToString(),
            layer = _uiLayerId
        };

        obj.AddComponent<RectTransform>();
        var tr = obj.transform;
        tr.SetParent(_uiRoot, false);
        tr.SetSiblingIndex((int)groupId - 1);
        
        var group = new UIGroup(tr);
        _layerGroupMap.Add(groupId, group);
        return group;
    } 
}

public sealed class LoadPanelArg : IResetable
{
    public uint LoadingId { get; private set; }
    public DUIPanel Cfg { get; private set; }
    public UIGroup Group { get; private set; }
    public object UserData { get; private set; }

    public static LoadPanelArg Create(uint loadingId, DUIPanel cfg, UIGroup group, object userData)
    {
        var info = ClassPool.Get<LoadPanelArg>();
        info.LoadingId = loadingId;
        info.Cfg = cfg;
        info.Group = group;
        info.UserData = userData;
        
        return info;
    }
    
    public void Reset()
    {
        LoadingId = 0;
        Cfg = null;
        Group = null;
        UserData = null;
    }
}