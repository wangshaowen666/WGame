/*--------------------------------------------------------------
 * File: EntityPool.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/26 10:30:00
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class EntityPool : ManagerBase
{
    /// <summary>闲置实体自动销毁并卸载资源的时间（秒）</summary>
    private const float AutoReleaseSec = 10f;

    private ObjectPool<GameObject> _pool = CoreMgr.ObjectPool.RegisterPool<GameObject>(AutoReleaseSec);
    private Transform _root; 

    /// <summary>按实体配置 Id 取实体表现对象（#Entity.xlsx）</summary>
    public void Acquire(int entityId, Transform parent, Action<GameObject> callback)
    {
        var cfg = GameMgr.DataTable.TbEntity[entityId];
        if (cfg == null)
        {
            Log.Error("不存在的实体配置：", entityId);
            return;
        }

        var key = cfg.Name;
        var obj = _pool.GetObj(key);
        if (obj == null)
        {
            CoreMgr.Res.LoadAsync<GameObject>(key, OnLoadFinish, LoadEntityArg.Create(key, parent, callback));
            return;
        }

        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;
        obj.SetActive(true);
        callback?.Invoke(obj);
    }

    /// <summary>按实体配置 Id 归还实体</summary>
    public void Release(int entityId, GameObject obj)
    {
        var cfg = GameMgr.DataTable.TbEntity[entityId];
        if (cfg == null)
        {
            Log.Error("不存在的实体配置：", entityId);
            Object.Destroy(obj);
            return;
        }
        if (obj == null) return;

        obj.transform.SetParent(Root, false);
        obj.transform.localPosition = Vector3.zero;
        obj.SetActive(false);
        _pool.PutObj(cfg.Name, obj);
    }

    /// <summary>
    /// 离开场景：实体不跨场景存活，销毁池根并整体释放（含 Addressables 卸载）。
    /// 注册顺序须在 BattleMgr 之前，保证战斗先 Dispose 归还实体、本池再清理。
    /// </summary>
    public override void OnSceneExit(string sceneNm)
    {
        if (_root != null)
        {
            Object.Destroy(_root.gameObject);
            _root = null;
        }

        // 释放并重建池（Release 会停掉自动释放计时器，重建恢复闲置回收能力）
        CoreMgr.ObjectPool.ReleasePool<GameObject>();
        _pool = CoreMgr.ObjectPool.RegisterPool<GameObject>(AutoReleaseSec);
    }

    private Transform Root
    {
        get
        {
            if (_root == null)
                _root = new GameObject("EntityPool").transform;
            return _root;
        }
    }

    private void OnLoadFinish(GameObject obj, object userData)
    {
        var arg = userData as LoadEntityArg;
        if (arg == null)
            throw new Exception("实体加载参数无效");

        // 加载期间父节点已被销毁（战斗结束/切场景）：放弃实例化，直接卸载
        if (arg.Parent == null)
        {
            Log.Warning("[EntityPool] 加载完成时父节点已销毁，丢弃实体：", arg.Key);
            if (obj != null)
                CoreMgr.Res.Unload(arg.Key);
            CoreMgr.ClassPool.Recycle(arg);
            return;
        }

        var entity = obj != null ? Object.Instantiate(obj, arg.Parent) : null;
        if (entity == null)
            Log.Error("[EntityPool] 实体实例化失败：", obj == null ? "<加载失败>" : obj.name);

        arg.Callback?.Invoke(entity);
        CoreMgr.ClassPool.Recycle(arg);
    }
}

public sealed class LoadEntityArg : IResetable
{
    public string Key;
    public Transform Parent;
    public Action<GameObject> Callback;

    public static LoadEntityArg Create(string key, Transform parent, Action<GameObject> callback)
    {
        var arg = CoreMgr.ClassPool.Get<LoadEntityArg>();
        arg.Key = key;
        arg.Parent = parent;
        arg.Callback = callback;
        return arg;
    }

    public void Reset()
    {
        Key = null;
        Parent = null;
        Callback = null;
    }
}
