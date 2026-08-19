/*--------------------------------------------------------------
 * File: BattleTD.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/18 17:00:00
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 帧同步塔防（表现层）：
/// - 订阅 FrameSyncMgr.OnFrame，把帧喂给 BattleSim（确定性模拟层），本类只做视图同步与日志
/// - 进场时用空帧快进到服务器当前帧（前提：进场前无人放塔；阶段 6 由 StartGame 消息统一进场帧根治）
/// - 视图用运行时图元（方块/圆柱），确定性验证优先，后续再接实体 prefab
/// - 每 50 帧打印状态哈希，双端对比哈希序列即可验证确定性
/// </summary>
public class BattleTD : BattleBase
{
    private BattleSim _sim;
    private Transform _root;  // 变换层（复用场景相机时挂到相机下做适配变换）
    private Transform _world; // 物体层（子物体统一用格子局部坐标，_world 负责居中）
    private GameObject _cameraGo; // 自建俯视相机（仅场景无 BattleCamera 时创建，Dispose 时销毁）
    private readonly List<Camera> _disabledCameras = new(); // 战斗期间禁用的场景相机（Dispose 恢复）
    private readonly Dictionary<int, Transform> _enemyViews = new();
    private readonly Dictionary<int, Transform> _towerViews = new();
    private bool _overLogged;
    private string _lastReject = ""; // 上一次的拒绝原因（用于变化检测）

    public override void Init()
    {
        _sim = new BattleSim();

        // 空帧快进：把进场前的历史帧以"无操作"方式补算，保证后进场客户端与先进场一致
        var empty = new NetMsg.PlayerInput[0];
        for (int n = 1; n <= GameMgr.FrameSync.CurFrameId; n++)
            _sim.Tick(n, empty);

        var cam = InitCamera();
        InitViews();

        // 复用场景相机（_cameraGo == null）时，把战场挂到相机下适配其视野
        if (_cameraGo == null && cam != null)
            AdaptWorldToCamera(cam);

        GameMgr.FrameSync.OnFrame += OnFrame;
        Log.Info("[战斗] 进入战斗, 已快进至帧", GameMgr.FrameSync.CurFrameId);
    }

    public override void Dispose()
    {
        GameMgr.FrameSync.OnFrame -= OnFrame;

        // 恢复被禁用的场景相机
        for (int i = 0; i < _disabledCameras.Count; i++)
            if (_disabledCameras[i] != null)
                _disabledCameras[i].enabled = true;
        _disabledCameras.Clear();

        if (_cameraGo != null)
        {
            Object.Destroy(_cameraGo);
            _cameraGo = null;
        }
        if (_root != null)
        {
            // 先脱离相机层级再销毁（复用场景相机时 _root 挂在相机下）
            _root.SetParent(null);
            Object.Destroy(_root.gameObject);
            _root = null;
            _world = null;
        }
        _enemyViews.Clear();
        _towerViews.Clear();
        _sim = null;
    }

    /// <summary>
    /// 收到逻辑帧：喂给模拟层并同步表现
    /// </summary>
    private void OnFrame(NetMsg.FrameData frame)
    {
        _sim.Tick(frame.FrameId, frame.Inputs);

        // 操作被拒原因（变化时输出，方便测试时看出"为什么放塔/升级没生效"）
        if (_sim.LastReject != _lastReject)
        {
            _lastReject = _sim.LastReject;
            if (!string.IsNullOrEmpty(_lastReject))
                Log.Warning("[操作被拒]", _lastReject);
        }

        SyncViews();

        // 每 50 帧（2.5 秒）输出哈希校验行，双端对比
        if (frame.FrameId % 50 == 0)
            Log.Info("[校验] 帧", frame.FrameId, GetDebugState());

        if (_sim.GameOver && !_overLogged)
        {
            _overLogged = true;
            Log.Error("[战斗] 基地被攻破, 游戏失败 @帧", frame.FrameId);
        }
    }

    /// <summary>当前被拒原因（最近一次，空=无；供测试 UI 显示）</summary>
    public string RejectText => _lastReject;

    /// <summary>
    /// 调试信息：哈希 + 关键计数（金币串按 Players 列表顺序拼接，双端一致）
    /// </summary>
    public string GetDebugState()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("哈希:").Append(_sim.StateHash().ToString("x8"))
          .Append(" 敌:").Append(_sim.Enemies.Count)
          .Append(" 塔:").Append(_sim.Towers.Count)
          .Append(" 基地:").Append(_sim.BaseHp)
          .Append(" 金币:");
        for (int i = 0; i < _sim.Players.Count; i++)
        {
            if (i > 0) sb.Append('/');
            sb.Append("P").Append(_sim.Players[i].PlayerId).Append(':').Append(_sim.Players[i].Gold);
        }
        return sb.ToString();
    }

    // ---------------- 视图 ----------------

    /// <summary>
    /// 战斗相机策略（按优先级）：
    /// 1) 复用场景里已配置的 BattleCamera（Main 场景，画面已验证正确），战场物体由 AdaptWorldToCamera 适配其视野；
    ///    保留 UI 相机（界面渲染必需），禁用其余相机
    /// 2) 场景没有 BattleCamera（如 Editor Test 场景）时自建俯视相机（禁用场景全部相机），
    ///    物体保持世界格子坐标（_root 在场景根，local == world）
    /// </summary>
    private Camera InitCamera()
    {
        var existing = Object.FindObjectsOfType<Camera>();

        // 优先找场景战斗相机
        Camera sceneCam = null;
        for (int i = 0; i < existing.Length; i++)
            if (existing[i].name == "BattleCamera")
            {
                sceneCam = existing[i];
                break;
            }

        _disabledCameras.Clear();
        for (int i = 0; i < existing.Length; i++)
        {
            var c = existing[i];
            if (c == sceneCam) continue;
            if (c.name.Contains("UI")) continue; // UI 相机渲染界面，不能禁

            Log.Info("[战斗] 禁用场景相机:", c.name, "depth=", c.depth, "clear=", c.clearFlags);
            c.enabled = false;
            _disabledCameras.Add(c);
        }

        if (sceneCam != null)
        {
            Log.Info("[战斗] 复用场景相机 BattleCamera, 战场将适配其视野");
            return sceneCam;
        }

        // fallback：自建俯视相机（竖屏 1440×3200 aspect 0.45 FOV 60 下，h=16 可见 8.3×18.5，8×16 地图填满屏幕）
        _cameraGo = new GameObject("BattleCamera");
        var cam = _cameraGo.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.06f, 0.10f);
        cam.depth = 10f;
        cam.transform.SetPositionAndRotation(
            new Vector3(BattleSim.MapW / 2f, 16f, BattleSim.MapH / 2f),
            Quaternion.Euler(90f, 0f, 0f)); // 垂直下看
        return cam;
    }

    /// <summary>
    /// 把战场挂到场景相机下并适配其视野：
    /// 地图放在相机正前方 dist 米处（XZ 平面正对镜头，长边朝画面上方），
    /// 按该距离的可视范围计算等比缩放，让整张地图带边距地填满画面
    /// </summary>
    private void AdaptWorldToCamera(Camera cam)
    {
        const float dist = 20f;
        var fovRad = cam.fieldOfView * Mathf.Deg2Rad;
        var visH = 2f * dist * Mathf.Tan(fovRad / 2f); // dist 处垂直可视高度
        var visW = visH * cam.aspect;                  // 水平可视宽度
        var scale = Mathf.Min(visW / BattleSim.MapW, visH / BattleSim.MapH) * 0.92f; // 留 8% 边距

        _root.SetParent(cam.transform, false);
        _root.localPosition = new Vector3(0, 0, dist);
        _root.localRotation = Quaternion.Euler(-90f, 0f, 0f); // 地图 +Y（长边）指向画面上方
        _root.localScale = Vector3.one * scale;

        // 地图中心对齐 _root 原点（子物体保持格子坐标，由 _world 统一居中）
        _world.localPosition = new Vector3(-BattleSim.MapW / 2f, 0, -BattleSim.MapH / 2f);

        Log.Info("[战斗] 适配场景相机: fov=", cam.fieldOfView, "aspect=", cam.aspect.ToString("F2"),
            "scale=", scale.ToString("F3"));
    }

    private void InitViews()
    {
        _root = new GameObject("BattleTD").transform;
        // 物体层：子物体统一用格子局部坐标；复用场景相机时由 _world 负责把地图中心对齐 _root 原点
        _world = new GameObject("World").transform;
        _world.SetParent(_root, false);

        // 地板
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(_world, false);
        floor.transform.localScale = new Vector3(BattleSim.MapW / 10f, 1, BattleSim.MapH / 10f);
        floor.transform.localPosition = new Vector3(BattleSim.MapW / 2f, 0, BattleSim.MapH / 2f);
        SetMat(floor.GetComponent<Renderer>(), new Color(0.18f, 0.22f, 0.18f));

        // 路径点标记（黄色）
        foreach (var wp in BattlePath())
        {
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mark.name = "Waypoint";
            mark.transform.SetParent(_world, false);
            mark.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
            mark.transform.localPosition = new Vector3(wp.x + 0.5f, 0.06f, wp.y + 0.5f);
            SetMat(mark.GetComponent<Renderer>(), Color.yellow);
        }

        // 终点基地（绿色）
        var last = BattlePath()[BattlePath().Length - 1];
        var home = GameObject.CreatePrimitive(PrimitiveType.Cube);
        home.name = "Home";
        home.transform.SetParent(_world, false);
        home.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        home.transform.localPosition = new Vector3(last.x + 0.5f, 0.45f, last.y + 0.5f);
        SetMat(home.GetComponent<Renderer>(), Color.green);
    }

    /// <summary>路径点格子坐标（与 BattleSim 内保持一致，阶段 6 随 StartGame 下发时统一收口）</summary>
    private static Vector2Int[] BattlePath()
    {
        return new[]
        {
            new Vector2Int(0, 0), new Vector2Int(0, 6), new Vector2Int(3, 6),
            new Vector2Int(3, 11), new Vector2Int(6, 11), new Vector2Int(6, 15),
        };
    }

    // ---- URP 材质工厂 ----
    // CreatePrimitive 的默认材质是 Built-in Standard shader，URP 真机渲染为粉色（Editor 有兜底）；
    // 必须显式创建 URP 材质，且 URP Lit 颜色属性是 _BaseColor（Material.color 写 _Color 无效）
    private static readonly Dictionary<Color, Material> s_matCache = new();

    private static Material GetMat(Color c)
    {
        if (s_matCache.TryGetValue(c, out var m)) return m;

        // 1) 按名查找（要求 shader 已进主包：Always Included Shaders 或被主包材质引用）
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            m = new Material(shader);
        }
        else
        {
            // 2) 兜底：克隆当前渲染管线的默认材质。真机上项目所有材质都在 Addressables
            //    bundle 里，URP Lit shader 只进 bundle 不进主包，主包阶段 Shader.Find 查不到
            //    （会 null 导致 new Material 抛 ArgumentNullException）；
            //    管线默认材质随 URP 初始化必备，真机一定存在
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp != null && rp.defaultMaterial != null)
            {
                m = new Material(rp.defaultMaterial);
                Log.Info("[战斗] Shader.Find 未命中，已克隆管线默认材质");
            }
            else
            {
                // 3) 最后兜底：UGUI 用的 Sprites/Default（主场景有 UI，必在包内）
                m = new Material(Shader.Find("Sprites/Default"));
                Log.Warning("[战斗] 使用 Sprites/Default 兜底材质");
            }
        }

        m.SetColor("_BaseColor", c);
        m.SetColor("_Color", c); // Sprites/Standard 等 Built-in 系 shader 用 _Color，不存在的属性写入无效不报错
        s_matCache[c] = m;
        return m;
    }

    /// <summary>给图元设置 URP 材质（按颜色缓存，sharedMaterial 避免实例化泄漏）</summary>
    private static void SetMat(Renderer r, Color c)
    {
        r.sharedMaterial = GetMat(c);
    }

    /// <summary>
    /// 同步表现：为模拟层新增实体建视图，移除死亡实体，刷新位置
    /// </summary>
    private void SyncViews()
    {
        // 敌人
        for (int i = 0; i < _sim.Enemies.Count; i++)
        {
            var en = _sim.Enemies[i];
            if (!_enemyViews.TryGetValue(en.Id, out var view))
            {
                view = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                view.name = $"Enemy_{en.Id}";
                view.transform.SetParent(_world, false);
                view.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                SetMat(view.GetComponent<Renderer>(), Color.red);
                _enemyViews[en.Id] = view.transform;
            }
            view.transform.localPosition = new Vector3(en.X.AsFloat, 0.3f, en.Y.AsFloat);
        }
        RemoveDeadViews(_enemyViews);

        // 塔
        for (int i = 0; i < _sim.Towers.Count; i++)
        {
            var tw = _sim.Towers[i];
            if (!_towerViews.TryGetValue(tw.Id, out var view))
            {
                view = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
                view.name = $"Tower_{tw.Id}";
                view.transform.SetParent(_world, false);
                view.transform.localScale = new Vector3(0.7f, 0.4f, 0.7f);
                SetMat(view.GetComponent<Renderer>(), Color.blue);
                _towerViews[tw.Id] = view.transform;
            }
            view.transform.localPosition = new Vector3(tw.CellX + 0.5f, 0.4f, tw.CellY + 0.5f);
        }
        RemoveDeadViews(_towerViews);
    }

    /// <summary>
    /// 销毁模拟层已不存在的实体视图（倒序遍历字典键，可安全删除）
    /// </summary>
    private void RemoveDeadViews(Dictionary<int, Transform> views)
    {
        if (views.Count == 0) return;
        var aliveIds = new List<int>();
        // 复用收集：敌人与塔各自调用，用视图字典与模拟层对账
        if (views == _enemyViews)
            for (int i = 0; i < _sim.Enemies.Count; i++) aliveIds.Add(_sim.Enemies[i].Id);
        else
            for (int i = 0; i < _sim.Towers.Count; i++) aliveIds.Add(_sim.Towers[i].Id);

        var toRemove = new List<int>();
        foreach (var id in views.Keys)
            if (!aliveIds.Contains(id))
                toRemove.Add(id);
        for (int i = 0; i < toRemove.Count; i++)
        {
            if (views.TryGetValue(toRemove[i], out var view))
                Object.Destroy(view.gameObject);
            views.Remove(toRemove[i]);
        }
    }
}
