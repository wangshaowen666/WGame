/*--------------------------------------------------------------
 * File: ViewPool.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/26 10:30:00
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 表现层视图池（由旧 EntityMgr 降级改造而来，原"实体 = 逻辑寄生 MonoBehaviour"的范式已废弃）：
/// 只负责视图 GameObject 的"异步加载 + 池化复用"，不含任何逻辑--
/// 视图必须哑，坐标与表现由 ViewSync 对账驱动，禁止挂带逻辑的组件自我驱动。
/// 资源 key 沿用 #Entity.xlsx（Id -> Addressables 地址）导表工作流。
/// 回调时机不保证同步（池缓存命中时同步、首次加载异步），调用方需容忍"视图晚到"，
/// 与 ViewSync 的 spawn 返回 null + Attach 补挂模式配合使用。
/// </summary>
public class ViewPool : ManagerBase
{
    /// <summary>闲置视图自动销毁并卸载资源的时间（秒）</summary>
    private const float AutoReleaseSec = 10f;

    private ObjectPool<GameObject> _pool = CoreMgr.ObjectPool.RegisterPool<GameObject>(AutoReleaseSec);
    private Transform _root; // 池中闲置视图的挂载根（脱离战斗根，避免战斗销毁误伤池对象）

    /// <summary>按视图配置 Id 取视图（#Entity.xlsx）</summary>
    public void Acquire(int viewId, Transform parent, Action<GameObject> callback)
    {
        var cfg = GameMgr.DataTable.TbEntity[viewId];
        if (cfg == null)
        {
            Log.Error("不存在的视图配置：", viewId);
            return;
        }
        Acquire(cfg.Name, parent, callback);
    }

    /// <summary>
    /// 按资源 key 取视图：池中有缓存则同步回调；否则异步加载 prefab 实例化后回调。
    /// 注意：回调里只做表现初始化（姿态/表现参数），禁止写逻辑。
    /// </summary>
    public void Acquire(string key, Transform parent, Action<GameObject> callback)
    {
        var obj = _pool.GetObj(key);
        if (obj == null)
        {
            CoreMgr.Res.LoadAsync<GameObject>(key, OnLoadFinish, LoadViewArg.Create(key, parent, callback));
            return;
        }

        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;
        obj.SetActive(true);
        callback?.Invoke(obj);
    }

    /// <summary>按视图配置 Id 归还视图</summary>
    public void Release(int viewId, GameObject obj)
    {
        var cfg = GameMgr.DataTable.TbEntity[viewId];
        if (cfg == null)
        {
            Log.Error("不存在的视图配置：", viewId);
            Object.Destroy(obj);
            return;
        }
        Release(cfg.Name, obj);
    }

    /// <summary>按资源 key 归还视图：隐藏并挂回池根，等待复用或闲置自动销毁</summary>
    public void Release(string key, GameObject obj)
    {
        if (obj == null) return;

        obj.transform.SetParent(Root, false);
        obj.transform.localPosition = Vector3.zero;
        obj.SetActive(false);
        _pool.PutObj(key, obj);
    }

    /// <summary>
    /// 离开场景：视图不跨场景存活，销毁池根并整体释放（含 Addressables 卸载）。
    /// 注册顺序须在 BattleMgr 之前，保证战斗先 Dispose 归还视图、本池再清理。
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
                _root = new GameObject("ViewPool").transform;
            return _root;
        }
    }

    private void OnLoadFinish(GameObject obj, object userData)
    {
        var arg = userData as LoadViewArg;
        if (arg == null)
            throw new Exception("视图加载参数无效");

        // 加载期间父节点已被销毁（战斗结束/切场景）：放弃实例化，直接卸载
        if (arg.Parent == null)
        {
            Log.Warning("[ViewPool] 加载完成时父节点已销毁，丢弃视图：", arg.Key);
            if (obj != null)
                CoreMgr.Res.Unload(arg.Key);
            CoreMgr.ClassPool.Recycle(arg);
            return;
        }

        var view = obj != null ? Object.Instantiate(obj, arg.Parent) : null;
        if (view == null)
            Log.Error("[ViewPool] 视图实例化失败：", obj == null ? "<加载失败>" : obj.name);

        arg.Callback?.Invoke(view);
        CoreMgr.ClassPool.Recycle(arg);
    }
}

public sealed class LoadViewArg : IResetable
{
    public string Key;
    public Transform Parent;
    public Action<GameObject> Callback;

    public static LoadViewArg Create(string key, Transform parent, Action<GameObject> callback)
    {
        var arg = CoreMgr.ClassPool.Get<LoadViewArg>();
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
