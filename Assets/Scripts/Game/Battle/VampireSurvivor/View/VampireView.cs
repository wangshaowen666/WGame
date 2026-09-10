/*--------------------------------------------------------------
 * File: VampireView.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/08/31
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

/// <summary>
/// 吸血鬼幸存者（表现层）：
/// - 创建 VampireLogic 并由 LocalDriver 以固定逻辑帧驱动，每帧 OnFrame 触发视图对账
/// - 移动输入：GameJoystick 由战斗 UI（BattlePanel）登记到 GameMgr.Battle，经查缓存 + 就绪事件获取（解耦）
/// - 玩家视图经 ViewSync 对账（缺则建/多则销/刷坐标），读逻辑层数据只允许 Fix.AsFloat
/// - 本类只做组装与帧回调编排，拆分文件：实体视图三件套见 VampireView.Entities(partial)、
///   一次性表现（飘字/特效/死亡计时）见 VampireView.Fx(partial)、HUD 与选牌/结算面板见 VsHudPresenter、相机见 VsFollowCamera
/// </summary>
public partial class VampireView : BattleView
{
    private VampireLogic _logic;
    private LocalDriver _driver;
    private ViewSync<LogicHero, HeroView> _playerViews;
    private ViewSync<LogicEnemy, EnemyView> _enemyViews;
    private ViewSync<LogicProjectile, ProjectileView> _boltViews;
    private ViewSync<LogicDrop, DropView> _dropViews;
    private bool _inputBound;

    private VsHudPresenter _presenter; // HUD 与选牌/结算面板流程
    private VsFollowCamera _camera;    // 战斗跟随相机

    private Transform _entityRoot;

    // ---- 表现实体 id（Init 读配置表派生，逻辑实体不携带表现字段）----
    // 英雄/弹体/命中特效锚点：VampireLogic 公开常量 → 角色/武器表 → TbEntity 资源 id；敌人按敌种 CfgId 反查
    private int _heroEntityId;
    private int _boltEntityId;
    private int _boltHitEffectId;
    private int _damageTextEntityId;
    private int _gemEntityId;

    private EnemyHpBarRenderer _hpBars; // 敌人头顶血条合并渲染器（2-12，挂 EntityRoot 下）

    public override void Init()
    {
        _entityRoot = new GameObject("EntityRoot").transform;
        _entityRoot.transform.localEulerAngles = new Vector3(90, 0, 0);

        _hpBars = _entityRoot.gameObject.AddComponent<EnemyHpBarRenderer>(); // 敌人头顶血条（单动态 Mesh 合并）

        var seed = GenerateSeed();
        _logic = new VampireLogic(seed);
        _driver = new LocalDriver(_logic);
        _presenter = new VsHudPresenter(_logic, _driver);
        _camera = new VsFollowCamera();

        // 表现实体 id 反查（表链：角色→实体、关卡→飘字→实体、角色初始武器→弹体/命中特效实体、波次→敌种→宝石）
        var characterCfg = GameMgr.DataTable.TbVSCharacter.Get(VampireLogic.HeroCfgId);
        var stageCfg = GameMgr.DataTable.TbVSStage.Get(VampireLogic.StageId);
        _heroEntityId = characterCfg.EntityId;
        var weaponCfg = GameMgr.DataTable.TbVSWeapon.Get(characterCfg.StartWeaponId);
        _boltEntityId = weaponCfg.EntityId;
        _boltHitEffectId = weaponCfg.HitEffectId;
        _damageTextEntityId = stageCfg.DamageTextEntityId;

        // 宝石实体：取本关首个波次行敌种的掉落实体（当前各敌种共用同款宝石，分档 7-3 治理）
        var waves = GameMgr.DataTable.TbVSWave.DataList;
        for (int i = 0; i < waves.Count; i++)
        {
            if (waves[i].StageId != VampireLogic.StageId)
                continue;
            _gemEntityId = GameMgr.DataTable.TbVSEnemy.Get(waves[i].EnemyId).DropId;
            break;
        }

        if (GameMgr.Battle.Joystick != null)
            BindJoystickInput(GameMgr.Battle.Joystick);
        GameMgr.Event.Register<GameJoystick>(GameEvent.VsJoystickReady, OnVsJoystickReady);
        // 选牌确认事件（VsLevelUpChosen）由 VsHudPresenter 自注册/注销

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

        _camera.Init();

        Log.Info("[吸血鬼] 进入战斗, 种子:", seed);
    }

    /// <summary>逻辑帧推进：视图对账（帧状态推进 PushFrame）+ 消费命中事件（飘字+特效；致死敌人已被清扫，按死亡位置触发）+ HUD/面板流程</summary>
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

        _presenter.OnFrame(); // HUD 刷新 + 升级选牌（2-8）/死亡结算（2-11）面板流程
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

        _hpBars?.Refresh(enemies); // 敌人头顶血条：按已插值位置合并重绘（满血/死亡即隐藏）

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
        _presenter?.Dispose(); // 注销选牌事件 + 关闭选牌/结算面板（幂等）
        _presenter = null;
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

        _camera?.Dispose(); // 清理 Cinemachine：销毁虚拟相机，移除本战斗添加的 Brain
        _camera = null;

        _driver?.Dispose();
        _driver = null;
        _logic?.Dispose();
        _logic = null;
        Log.Info("[吸血鬼] 退出战斗，驱动器与逻辑层已清理");
    }

    /// <summary>暂停/恢复逻辑步进（直接停步进，不动 TimeScale）</summary>
    public void Pause() => _driver?.Pause();
    public void Resume() => _driver?.Resume();

    private static long GenerateSeed()
    {
        return DateTime.UtcNow.Ticks;
    }
}
