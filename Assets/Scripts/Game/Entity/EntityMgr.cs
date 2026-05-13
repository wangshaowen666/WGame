/*--------------------------------------------------------------
 * File: EntityMgr.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/27 10:11:47 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityMgr : Singleton<EntityMgr>
{
    private EntityMgr() { }

    private ObjectPool<EntityBase> _pool;
    private List<uint> _loadingIdMap = new List<uint>();

    protected override void OnInit()
    {
        base.OnInit();
        _pool = ObjectMgr.Instance.RegisterPool<EntityBase>(10);
    }

    public void CreateEntity(int eId, Transform parent, Action<EntityBase> callback)
    {
        var cfg = DataTableMgr.Instance.TbEntity[eId];
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
            ResMgr.Instance.LoadAsync<GameObject>(nm, OnLoadFinish, arg);
        }
        else
        {
            obj.transform.localPosition = Vector3.zero;
            callback?.Invoke(obj);
        }
    }

    public void RecycleEntity(int eId, EntityBase entity)
    {
        var cfg = DataTableMgr.Instance.TbEntity[eId];
        entity.transform.localPosition = Vector3.one * 9999;
        entity.OnRecycle();
        _pool.PutObj(cfg.Name, entity);
    }

    private void OnLoadFinish(GameObject obj, object userData)
    {
        LoadEntityArg arg = userData as LoadEntityArg;
        if (arg == null)
            throw new GameException("打开界面参数无效");

        var hasID = _loadingIdMap.Remove(arg.LoadingID);
        if (!hasID) return;
        
        var prefab = UnityEngine.Object.Instantiate(obj, arg.Parent);
        if (prefab == null)
            throw new GameException("实体预制体上缺少EntityBase脚本");
        
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