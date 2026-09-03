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
    private ViewSync<LogicProjectile, ProjectileView> _boltViews;
    private ViewSync<LogicDrop, DropView> _dropViews;
    private bool _inputBound;

    private Transform _entityRoot;

    // ---- 表现实体 id（Init 读配置表派生，逻辑实体不携带表现字段）----
    // 英雄/关卡怪种/弹体/命中特效锚点：VampireLogic 公开常量 → 角色/关卡/武器表 → TbEntity 资源 id
    private int _heroEntityId;
    private int _enemyEntityId;
    private int _boltEntityId;
    private int _boltHitEffectId;
    private int _damageTextEntityId;
    private int _gemEntityId;

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

        // 表现实体 id 反查（表链：角色→实体、关卡→怪种/飘字→实体、角色初始武器→弹体/命中特效实体）
        var characterCfg = GameMgr.DataTable.TbVSCharacter.Get(VampireLogic.HeroCfgId);
        var stageCfg = GameMgr.DataTable.TbVSStage.Get(VampireLogic.StageId);
        _heroEntityId = characterCfg.EntityId;
        _enemyEntityId = GameMgr.DataTable.TbVSEnemy.Get(stageCfg.EnemyId).EntityId;
        var weaponCfg = GameMgr.DataTable.TbVSWeapon.Get(characterCfg.StartWeaponId);
        _boltEntityId = weaponCfg.EntityId;
        _boltHitEffectId = weaponCfg.HitEffectId;
        _damageTextEntityId = stageCfg.DamageTextEntityId;
        _gemEntityId = GameMgr.DataTable.TbVSEnemy.Get(stageCfg.EnemyId).DropId;

        if (GameMgr.Battle.Joystick != null)
            BindJoystickInput(GameMgr.Battle.Joystick);
        GameMgr.Event.Register<GameJoystick>(GameEvent.VsJoystickReady, OnVsJoystickReady);

        _playerViews = new ViewSync<LogicHero, HeroView>(
            p => p.Id, SpawnHeroView, RefreshHeroView, DespawnHeroView);
        _enemyViews = new ViewSync<LogicEnemy, EnemyView>(
            e => e.Id, SpawnEnemyView, RefreshEnemyView, DespawnEnemyView);
        _boltViews = new ViewSync<LogicProjectile, ProjectileView>(
            b => b.Id, SpawnBoltView, RefreshBoltView, DespawnBoltView);
        _dropViews = new ViewSync<LogicDrop, DropView>(
            d => d.Id, SpawnDropView, RefreshDropView, DespawnDropView);
        
        _driver.OnFrame += OnFrame;
        _driver.OnRenderFrame += OnRenderFrame; // 渲染帧插值驱动（表现帧率与逻辑帧率解耦）

        InitFollowCamera();

        Log.Info("[吸血鬼] 进入战斗, 种子:", seed);
    }

    /// <summary>逻辑帧推进：视图对账（帧状态推进 PushFrame）+ 消费命中事件（飘字+特效；致死敌人已被清扫，按死亡位置触发）</summary>
    private void OnFrame(int frame)
    {
        _playerViews.Sync(_logic.Heroes);
        _enemyViews.Sync(_logic.Enemies);
        _boltViews.Sync(_logic.Projectiles);
        _dropViews.Sync(_logic.Drops);

        var hits = _logic.HitEvents;
        for (int i = 0; i < hits.Count; i++)
        {
            var x = hits[i].X.AsFloat;
            var y = hits[i].Y.AsFloat;
            SpawnHitEffect(x, y);
            SpawnDamageText(x, y, hits[i].Damage);
        }
    }

    /// <summary>渲染帧推进：按逻辑帧推进进度插值实体位置 + 飘字动画</summary>
    private void OnRenderFrame(float alpha, float deltaSeconds)
    {
        var heroes = _playerViews.ViewList;
        for (int i = 0; i < heroes.Count; i++)
            heroes[i].ApplyInterpolation(alpha);

        var enemies = _enemyViews.ViewList;
        for (int i = 0; i < enemies.Count; i++)
            enemies[i].ApplyInterpolation(alpha);

        var bolts = _boltViews.ViewList;
        for (int i = 0; i < bolts.Count; i++)
            bolts[i].ApplyInterpolation(alpha);

        var drops = _dropViews.ViewList;
        for (int i = 0; i < drops.Count; i++)
            drops[i].ApplyInterpolation(alpha);

        // 飘字动画（倒序：到期回池移除）
        for (int i = _activeTexts.Count - 1; i >= 0; i--)
        {
            var text = _activeTexts[i];
            text.Tick(deltaSeconds);
            _textLifes[i] -= deltaSeconds;
            if (_textLifes[i] > 0f)
                continue;

            GameMgr.EntityPool.Release(text.EntityId, text.gameObject);
            _activeTexts.RemoveAt(i);
            _textLifes.RemoveAt(i);
        }
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
        _driver.OnRenderFrame -= OnRenderFrame;
        GameMgr.Event.UnRegister<GameJoystick>(GameEvent.VsJoystickReady, OnVsJoystickReady);
        _playerViews?.Clear(); // 销毁全部玩家视图
        _playerViews = null;
        _enemyViews?.Clear(); // 归还全部敌人视图（走 DespawnEnemyView 进入死亡计时）
        _enemyViews = null;
        CancelDyingEnemies(); // 取消剩余死亡计时并立即归还
        CancelActiveEffects(); // 取消命中特效计时并立即归还
        CancelActiveTexts(); // 飘字全部立即归还
        _boltViews?.Clear(); // 归还全部弹幕视图（走 DespawnBoltView 回实体池）
        _boltViews = null;
        _dropViews?.Clear(); // 归还全部掉落视图（走 DespawnDropView 回实体池）
        _dropViews = null;

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

    private HeroView SpawnHeroView(LogicHero p)
    {
        var id = p.Id;              // 快照实体 id（异步加载期间实体可能被清扫/池化复用）
        var entityId = _heroEntityId;
        var px = p.X.AsFloat;       // 快照位置（Attach 前推一帧，避免异步加载期间视图停在原点被插值渲染）
        var py = p.Y.AsFloat;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 英雄实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<HeroView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 英雄预制体缺少 HeroView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId); // 记录实体配置 Id（Despawn 归还时作池 key）
            view.PushFrame(px, py);
            _playerViews.Attach(id, view);
            // 英雄视图就绪后作为相机跟随目标（单机单人：首个英雄）
            if (_vcam != null && _vcam.Follow == null)
                _vcam.Follow = view.transform;
        });

        return null;
    }

    private void RefreshHeroView(LogicHero p, HeroView view)
    {
        view.PushFrame(p.X.AsFloat, p.Y.AsFloat);
        view.SetFlip(p.FacingX.AsFloat);
    }

    /// <summary>英雄视图消亡：归还实体池（复用，而非销毁）</summary>
    private void DespawnHeroView(HeroView view)
    {
        GameMgr.EntityPool.Release(view.EntityId, view.gameObject);
    }

    // ---- 敌人视图 ----

    private EnemyView SpawnEnemyView(LogicEnemy e)
    {
        var id = e.Id;             // 快照实体 id（异步加载期间实体可能被清扫/池化复用）
        var entityId = _enemyEntityId;
        var px = e.X.AsFloat;      // 快照位置（Attach 前推一帧，避免异步加载期间视图停在原点被插值渲染）
        var py = e.Y.AsFloat;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 敌人实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<EnemyView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 敌人预制体缺少 EnemyView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId); // 记录实体配置 Id（Despawn 归还时作池 key）
            view.PushFrame(px, py);
            _enemyViews.Attach(id, view);
        });

        return null;
    }

    private void RefreshEnemyView(LogicEnemy e, EnemyView view)
    {
        view.PushFrame(e.X.AsFloat, e.Y.AsFloat);
        view.SetFlip(e.FacingX.AsFloat);
        if (view.OnHitFrame(e.LastHitFrame))
            SpawnHitEffect(e.X.AsFloat, e.Y.AsFloat); // 新受击：在受击敌人位置触发命中特效
        view.TickFlash();
    }

    /// <summary>敌人视图消亡：播死亡动画，Timer 计时播完（0.5s）后回池</summary>
    private void DespawnEnemyView(EnemyView view)
    {
        view.PlayDie();
        _dyingViews[view] = CoreMgr.Timer.StartSecondDelay(EnemyDieAnimSeconds, () =>
        {
            _dyingViews.Remove(view);
            GameMgr.EntityPool.Release(view.EntityId, view.gameObject);
        });
    }

    // ---- 弹幕视图 ----

    private ProjectileView SpawnBoltView(LogicProjectile b)
    {
        var id = b.Id;
        var entityId = _boltEntityId;
        var dirX = b.DirX.AsFloat; // 快照方向（弹体池化复用后字段会被 Reset）
        var dirY = b.DirY.AsFloat;
        var px = b.X.AsFloat;      // 快照位置（Attach 前推一帧，避免异步加载期间视图停在原点被插值渲染）
        var py = b.Y.AsFloat;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 弹幕实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<ProjectileView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 弹幕预制体缺少 ProjectileView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId); // 记录实体配置 Id（Despawn 归还时作池 key）
            view.PushFrame(px, py);
            view.SetDirection(dirX, dirY); // 创建时朝向飞行目标（直线飞行，仅此一次）
            _boltViews.Attach(id, view);
        });

        return null;
    }

    private static void RefreshBoltView(LogicProjectile b, ProjectileView view)
    {
        view.PushFrame(b.X.AsFloat, b.Y.AsFloat);
    }

    /// <summary>弹幕视图消亡：归还实体池（复用，而非销毁；池 key 用创建时记录的实体配置 Id）</summary>
    private void DespawnBoltView(ProjectileView view)
    {
        GameMgr.EntityPool.Release(view.EntityId, view.gameObject);
    }

    // ---- 掉落物视图 ----

    private DropView SpawnDropView(LogicDrop d)
    {
        var id = d.Id;             // 快照实体 id（异步加载期间实体可能被清扫/池化复用）
        var entityId = _gemEntityId;
        var px = d.X.AsFloat;      // 快照位置（Attach 前推一帧）
        var py = d.Y.AsFloat;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 掉落实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<DropView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 掉落预制体缺少 DropView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId);
            view.PushFrame(px, py);
            _dropViews.Attach(id, view);
        });

        return null;
    }

    private static void RefreshDropView(LogicDrop d, DropView view)
    {
        view.PushFrame(d.X.AsFloat, d.Y.AsFloat);
    }

    /// <summary>掉落视图消亡（被拾取，2-7）：归还实体池</summary>
    private void DespawnDropView(DropView view)
    {
        GameMgr.EntityPool.Release(view.EntityId, view.gameObject);
    }

    // ---- 伤害飘字（一次性表现：命中时 Acquire → 上浮淡出（渲染帧驱动）→ 到期回池，不走 ViewSync）----

    private const float DamageTextLifeSeconds = 0.6f;
    private readonly List<DamageTextView> _activeTexts = new();
    private readonly List<float> _textLifes = new();

    private void SpawnDamageText(float x, float y, long damage)
    {
        var entityId = _damageTextEntityId;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 飘字实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<DamageTextView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 飘字预制体缺少 DamageTextView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId);
            view.Show(x, y, damage);
            _activeTexts.Add(view);
            _textLifes.Add(DamageTextLifeSeconds);
        });
    }

    /// <summary>退出战斗：飘字全部立即归还（无 Timer，无需取消）</summary>
    private void CancelActiveTexts()
    {
        for (int i = 0; i < _activeTexts.Count; i++)
            GameMgr.EntityPool.Release(_activeTexts[i].EntityId, _activeTexts[i].gameObject);
        _activeTexts.Clear();
        _textLifes.Clear();
    }

    // ---- 命中特效（一次性表现：命中时 Acquire → 播放 → Timer 计时回池，不走 ViewSync——逻辑层无特效实体）----

    private const float HitEffectSeconds = 0.3f; // 特效存活时长（对齐特效资产动画时长）
    private readonly Dictionary<EffectView, CancellationTokenSource> _activeEffects = new();

    private void SpawnHitEffect(float x, float y)
    {
        var entityId = _boltHitEffectId;
        GameMgr.EntityPool.Acquire(entityId, _entityRoot, (go) =>
        {
            if (go == null)
            {
                Log.Error("[吸血鬼] 命中特效实体加载失败, 实体Id:", entityId);
                return;
            }
            var view = go.GetComponent<EffectView>();
            if (view == null)
            {
                Log.Error("[吸血鬼] 特效预制体缺少 EffectView 组件, 实体Id:", entityId, "，已销毁");
                Object.Destroy(go);
                return;
            }
            view.SetEntityId(entityId);
            view.SetPosition(x, y);
            _activeEffects[view] = CoreMgr.Timer.StartSecondDelay(HitEffectSeconds, () =>
            {
                _activeEffects.Remove(view);
                GameMgr.EntityPool.Release(view.EntityId, view.gameObject);
            });
        });
    }

    /// <summary>退出战斗：取消特效回池计时并立即归还</summary>
    private void CancelActiveEffects()
    {
        foreach (var kv in _activeEffects)
        {
            CoreMgr.Timer.Stop(kv.Value);
            GameMgr.EntityPool.Release(kv.Key.EntityId, kv.Key.gameObject);
        }
        _activeEffects.Clear();
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
            GameMgr.EntityPool.Release(kv.Key.EntityId, kv.Key.gameObject);
        }
        _dyingViews.Clear();
    }

    private static long GenerateSeed()
    {
        return DateTime.UtcNow.Ticks;
    }
}
