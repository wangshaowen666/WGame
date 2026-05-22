/*--------------------------------------------------------------
 * File: EntityMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/27 10:11:47 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityMgr : ManagerBase
{
    private ObjectPool<EntityBase> _pool = FrameworkMgr.ObjectPool.RegisterPool<EntityBase>(10);
    private List<uint> _loadingIdMap = new List<uint>();

    public void CreateEntity(int eId, Transform parent, Action<EntityBase> callback)
    {
        var cfg = GameMgr.DataTable.TbEntity[eId];
        if (cfg == null)
        {
            Log.Error("不存在的实体：", eId);
            return;
        }
        
        var nm = cfg.Name;
        var obj = _pool.GetObj(nm);
        if (obj == null)
        {
            var loadingID = AutoID.GetID();
            _loadingIdMap.Add(loadingID);
            LoadEntityArg arg = LoadEntityArg.Create(loadingID, parent, callback);
            FrameworkMgr.Res.LoadAsync<GameObject>(nm, OnLoadFinish, arg);
        }
        else
        {
            obj.transform.localPosition = Vector3.zero;
            callback?.Invoke(obj);
        }
    }

    public void RecycleEntity(int eId, EntityBase entity)
    {
        // 先执行回收，对应碰撞实体 OnRecycle处理了碰撞关闭操作，
        entity.OnRecycle();
        
        var cfg = GameMgr.DataTable.TbEntity[eId];
        entity.transform.localPosition = Vector3.one * 9999;
        _pool.PutObj(cfg.Name, entity);
    }

    private void OnLoadFinish(GameObject obj, object userData)
    {
        LoadEntityArg arg = userData as LoadEntityArg;
        if (arg == null)
            throw new Exception("打开界面参数无效");

        var hasID = _loadingIdMap.Remove(arg.LoadingID);
        if (!hasID) return;
        
        var prefab = UnityEngine.Object.Instantiate(obj, arg.Parent);
        if (prefab == null)
            throw new Exception("实体预制体上缺少EntityBase脚本");
        
        var entity = prefab.GetComponent<EntityBase>();
        arg.Callback?.Invoke(entity);
        
        ClassPool.Recycle(arg);
    }
}

public sealed class LoadEntityArg : IResetable
{
    public uint LoadingID;
    public Transform Parent;
    public Action<EntityBase> Callback;

    public static LoadEntityArg Create(uint loadingID, Transform parent, Action<EntityBase> callback)
    {
        LoadEntityArg arg = ClassPool.Get<LoadEntityArg>();
        arg.LoadingID = loadingID;
        arg.Parent = parent;
        arg.Callback = callback;
            
        return arg;
    }
        
    public void Reset()
    {
        Parent = null;
        Callback = null;
    }
}