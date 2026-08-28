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

/// <summary>
/// 实体池（表现层，由旧 EntityMgr 改造而来）：
/// 池化的是游戏实体（角色/怪物/子弹/特效）的表现对象（prefab 实例），
/// 只负责"异步加载 + 池化复用"，不含任何逻辑--逻辑全在逻辑层（LogicXxx），
/// 实体对象只有表现，坐标与外观由 ViewSync 对账驱动，禁止挂带逻辑的组件自我驱动。
/// 资源 key 沿用 #Entity.xlsx（entityId -> Addressables 地址）导表工作流。
/// 回调时机不保证同步（池缓存命中时同步、首次加载异步），调用方需容忍"实体晚到"，
/// 与 ViewSync 的 spawn 返回 null + Attach 补挂模式配合使用。
/// </summary>
public class EntityPool : ManagerBase
{
    /// <summary>闲置实体自动销毁并卸载资源的时间（秒）</summary>
    private const float AutoReleaseSec = 10f;

    private ObjectPool<GameObject> _pool = CoreMgr.ObjectPool.RegisterPool<GameObject>(AutoReleaseSec);
    private Transform _root; // 池中闲置实体的挂载根（脱离战斗根，避免战斗销毁误伤池对象）

    /// <summary>按实体配置 Id 取实体表现对象（#Entity.xlsx）</summary>
    public void Acquire(int entityId, Transform parent, Action<GameObject> callback)
    {
        var cfg = GameMgr.DataTable.TbEntity[entityId];
        if (cfg == null)
        {
            Log.Error("不存在的实体配置：", entityId);
            return;
        }
        Acquire(cfg.Name, parent, callback);
    }

    /// <summary>
    /// 按资源 key 取实体：池中有缓存则同步回调；否则异步加载 prefab 实例化后回调。
    /// 注意：回调里只做表现初始化（姿态/外观参数），禁止写逻辑。
    /// </summary>
    public void Acquire(string key, Transform parent, Action<GameObject> callback)
    {
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
        Release(cfg.Name, obj);
    }

    /// <summary>按资源 key 归还实体：隐藏并挂回池根，等待复用或闲置自动销毁</summary>
    public void Release(string key, GameObject obj)
    {
        if (obj == null) return;

        obj.transform.SetParent(Root, false);
        obj.transform.localPosition = Vector3.zero;
        obj.SetActive(false);
        _pool.PutObj(key, obj);
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
