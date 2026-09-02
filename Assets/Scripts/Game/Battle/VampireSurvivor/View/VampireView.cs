/*--------------------------------------------------------------
 * File: VampireView.cs
 * Author: Wsw
 * Time: 2026/08/31
 *--------------------------------------------------------------
 */

using Cinemachine;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 吸血鬼幸存者（表现层）：
/// - 创建 VampireLogic 并由 LocalDriver 以固定逻辑帧驱动，每帧 OnFrame 触发视图对账
/// - 移动输入：GameJoystick 由战斗 UI（BattlePanel）登记到 GameMgr.Battle，经查缓存 + 就绪事件获取（解耦）
/// - 玩家视图经 ViewSync 对账（缺则建/多则销/刷坐标），读逻辑层数据只允许 Fix.AsFloat
/// </summary>
public class VampireView : BattleView
{
    private VampireLogic _logic;
    private LocalDriver _driver;
    private ViewSync<LogicHero, HeroView> _playerViews;
    private ViewSync<LogicEnemy, EnemyView> _enemyViews;
    private bool _inputBound;

    private Transform _entityRoot;

    private CinemachineVirtualCamera _vcam; // 玩家跟随虚拟相机
    private Camera _brainCamera;            // 本战斗为其添加 CinemachineBrain 的相机（退出时移除）
    private GameObject _ground;             // 地面（大平面覆盖移动范围）

    public override void Init()
    {
        _entityRoot = new GameObject("EntityRoot").transform;
        _entityRoot.transform.localEulerAngles = new Vector3(90, 0, 0);
        
        var seed = GenerateSeed();
        _logic = new VampireLogic(seed);
        _driver = new LocalDriver(_logic); 

        if (GameMgr.Battle.Joystick != null)
            BindJoystickInput(GameMgr.Battle.Joystick);
        GameMgr.Event.Register<GameJoystick>(GameEvent.VsJoystickReady, OnVsJoystickReady);

        _playerViews = new ViewSync<LogicHero, HeroView>(
            p => p.Id, SpawnHeroView, RefreshHeroView, DespawnHeroView);
        _enemyViews = new ViewSync<LogicEnemy, EnemyView>(
            e => e.Id, SpawnEnemyView, RefreshEnemyView, DespawnEnemyView);
        
        _driver.OnFrame += OnFrame;

        InitFollowCamera();

        Log.Info("[吸血鬼] 进入战斗, 种子:", seed);
    }

    /// <summary>逻辑帧推进：视图对账</summary>
    private void OnFrame(int frame)
    {
        _playerViews.Sync(_logic.Heroes);
        _enemyViews.Sync(_logic.Enemies);
    }

    private void OnVsJoystickReady(GameJoystick joystick)
    {
        BindJoystickInput(joystick);
    }

    private void BindJoystickInput(GameJoystick joystick)
    {
        if (_inputBound || joystick == null)
        {
            if (joystick == null)
                Log.Warning("[吸血鬼] 战斗摇杆为空，本局输入为空帧");
            return;
        }
        _driver.RegisterInput(new JoystickInput(joystick));
        _inputBound = true;
    }

    public override void Dispose()
    {
        _driver.OnFrame -= OnFrame;
        GameMgr.Event.UnRegister<GameJoystick>(GameEvent.VsJoystickReady, OnVsJoystickReady);
        _playerViews?.Clear(); // 销毁全部玩家视图
        _playerViews = null;
        _enemyViews?.Clear(); // 归还全部敌人视图（走 DespawnEnemyView 进入死亡计时）
        _enemyViews = null;
        CancelDyingEnemies(); // 取消剩余死亡计时并立即归还

        // 清理 Cinemachine：销毁虚拟相机，移除本战斗添加的 Brain（避免污染共享相机）
        if (_vcam != null)
        {
            Object.Destroy(_vcam.gameObject);
            _vcam = null;
        }
        if (_brainCamera != null)
        {
            var brain = _brainCamera.GetComponent<CinemachineBrain>();
            if (brain != null)
                Object.Destroy(brain);
            _brainCamera = null;
        }
        if (_ground != null)
        {
            Object.Destroy(_ground);
            _ground = null;
        }

        _driver?.Dispose();
        _driver = null;
        _logic?.Dispose();
        _logic = null;
        Log.Info("[吸血鬼] 退出战斗，驱动器与逻辑层已清理");
    }

    /// <summary>暂停/恢复逻辑步进（直接停步进，不动 TimeScale）</summary>
    public void Pause() => _driver?.Pause();
    public void Resume() => _driver?.Resume();

    // ---- 相机与地面 ----

    /// <summary>初始化 Cinemachine 相机跟随：主相机挂 Brain，创建虚拟相机（Transposer 俯视跟随，保持原相机视角）</summary>
    private void InitFollowCamera()
    {
        var cam = GameCamera.MainCamera ?? Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            Log.Warning("[吸血鬼] 未找到相机，跳过 Cinemachine 跟随");
            return;
        }

        if (cam.GetComponent<CinemachineBrain>() == null)
        {
            cam.gameObject.AddComponent<CinemachineBrain>();
            _brainCamera = cam;
        }

        var go = new GameObject("VampireFollowCamera");
        _vcam = go.AddComponent<CinemachineVirtualCamera>();
        _vcam.m_Priority = 20; // 高于默认，进入战斗即被 Brain 采用
        _vcam.m_Lens = LensSettings.FromCamera(cam); // 继承原相机镜头（透视/视野/裁剪面不变）

        // 俯视跟随：Body 用 Transposer（世界空间固定偏移，随英雄平移），不接管旋转
        //（无 Aim 组件 = DoNothing，相机保持下方设定的俯视朝向，不朝向 Follow）
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 对齐原相机俯视朝向
        var body = _vcam.AddCinemachineComponent<CinemachineTransposer>();
        body.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
        body.m_FollowOffset = new Vector3(0f, 10f, 5f); // 相对英雄的固定偏移（对齐原相机坐标 0,10,5）
        body.m_XDamping = 2f; // 平滑跟随（相机本地 X/Y = 世界水平面）
        body.m_YDamping = 2f;
        body.m_ZDamping = 0f; // 高度方向无阻尼，保持恒定偏移
    }

    // ---- 玩家视图 ----

    private const int HeroEntityId = 101001; // 英雄实体配置 Id（#Entity.xlsx）

    private HeroView SpawnHeroView(int id)
    {
        GameMgr.EntityPool.Acquire(HeroEntityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 英雄实体加载失败, 实体Id:", HeroEntityId);
                return;
            }
            var view = go.GetComponent<HeroView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 英雄预制体缺少 HeroView 组件, 实体Id:", HeroEntityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            _playerViews.Attach(id, view);
            // 英雄视图就绪后作为相机跟随目标（单机单人：首个英雄）
            if (_vcam != null && _vcam.Follow == null)
                _vcam.Follow = view.transform;
        });

        return null;
    }

    private static void RefreshHeroView(LogicHero p, HeroView view)
    {
        view.SetPosition(p.X.AsFloat, p.Y.AsFloat);
        view.SetFlip(p.FacingX.AsFloat);
    }

    /// <summary>英雄视图消亡：归还实体池（复用，而非销毁）</summary>
    private void DespawnHeroView(HeroView view)
    {
        GameMgr.EntityPool.Release(HeroEntityId, view.gameObject);
    }

    // ---- 敌人视图 ----

    private const int EnemyEntityId = 201001; // 敌人实体配置 Id（#Entity.xlsx）

    private EnemyView SpawnEnemyView(int id)
    {
        GameMgr.EntityPool.Acquire(EnemyEntityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 敌人实体加载失败, 实体Id:", EnemyEntityId);
                return;
            }
            var view = go.GetComponent<EnemyView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 敌人预制体缺少 EnemyView 组件, 实体Id:", EnemyEntityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            _enemyViews.Attach(id, view);
        });

        return null;
    }

    private static void RefreshEnemyView(LogicEnemy e, EnemyView view)
    {
        view.SetPosition(e.X.AsFloat, e.Y.AsFloat);
        view.SetFlip(e.FacingX.AsFloat);
    }

    /// <summary>敌人视图消亡：播死亡动画，Timer 计时播完（0.5s）后回池</summary>
    private void DespawnEnemyView(EnemyView view)
    {
        view.PlayDie();
        _dyingViews[view] = CoreMgr.Timer.StartSecondDelay(EnemyDieAnimSeconds, () =>
        {
            _dyingViews.Remove(view);
            GameMgr.EntityPool.Release(EnemyEntityId, view.gameObject);
        });
    }

    // ---- 敌人死亡动画回池计时 ----

    private const float EnemyDieAnimSeconds = 0.5f;
    private readonly Dictionary<EnemyView, CancellationTokenSource> _dyingViews = new();

    /// <summary>退出战斗：取消剩余死亡计时并立即归还（防回调迟到重复归还/视图泄漏）</summary>
    private void CancelDyingEnemies()
    {
        foreach (var kv in _dyingViews)
        {
            CoreMgr.Timer.Stop(kv.Value);
            GameMgr.EntityPool.Release(EnemyEntityId, kv.Key.gameObject);
        }
        _dyingViews.Clear();
    }

    private static long GenerateSeed()
    {
        return DateTime.UtcNow.Ticks;
    }
}
