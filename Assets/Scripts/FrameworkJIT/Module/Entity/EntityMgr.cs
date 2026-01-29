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

    private ObjectPool<GameObject> _pool;

    protected override void OnInit()
    {
        base.OnInit();
        
    }

    public void ShowEntity(int eId, Transform parent, Action<GameObject> callback)
    {
        var cfg = DataTableCtr.Instance.TbEntity[eId];
        if (cfg == null)
        {
            Log.Error("不存在的实体：", eId);
            return;
        }
        
        var nm = cfg.Name;
        var obj = _pool.GetObj(nm);
        if (obj == null)
        {
            LoadEntityArg arg = LoadEntityArg.Create(parent, callback);
            ResMgr.Instance.LoadAsync<GameObject>(nm, OnLoadFinish, arg);
        }
        else
        {
            
        }
    }

    private void OnLoadFinish(GameObject obj, object userData)
    {
        
    }
}

public sealed class LoadEntityArg : IResetable
{
    public Transform Parent;
    public Action<GameObject> Callback;

    public static LoadEntityArg Create(Transform parent, Action<GameObject> callback)
    {
        LoadEntityArg arg = ClassPool.Get<LoadEntityArg>();
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