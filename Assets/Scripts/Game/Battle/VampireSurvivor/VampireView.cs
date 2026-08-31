/*--------------------------------------------------------------
 * File: VampireView.cs
 * Author: Wsw
 * Time: 2026/08/31
 *--------------------------------------------------------------
 */

using System;
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
    private ViewSync<LogicPlayer, Transform> _playerViews; // 玩家视图对账器
    private bool _inputBound; // 摇杆输入是否已绑定（幂等，防重复登记/重复广播）

    public override void Init()
    {
        var seed = GenerateSeed();
        _logic = new VampireLogic(seed);
        _driver = new LocalDriver(_logic); // 注册 CoreMgr.Update，帧号从 1 递增驱动

        // 摇杆由战斗 UI 登记到 GameMgr.Battle，经查缓存 + 就绪事件获取（面板异步加载时序安全）
        if (GameMgr.Battle.Joystick != null)
            BindJoystickInput(GameMgr.Battle.Joystick);
        GameMgr.Event.Register<GameJoystick>(GameEvent.VsJoystickReady, OnVsJoystickReady);

        _playerViews = new ViewSync<LogicPlayer, Transform>(
            p => p.Id, SpawnPlayerView, RefreshPlayerView, DespawnView);
        _driver.OnFrame += OnFrame;

        Log.Info("[吸血鬼] 进入战斗, 种子:", seed);
    }

    /// <summary>逻辑帧推进：视图对账</summary>
    private void OnFrame(int frame)
    {
        _playerViews.Sync(_logic.Players);
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
        _driver?.Dispose();
        _driver = null;
        _logic?.Dispose();
        _logic = null;
        Log.Info("[吸血鬼] 退出战斗，驱动器与逻辑层已清理");
    }

    /// <summary>暂停/恢复逻辑步进（直接停步进，不动 TimeScale）</summary>
    public void Pause() => _driver?.Pause();
    public void Resume() => _driver?.Resume();

    // ---- 玩家视图 ----

    private Transform SpawnPlayerView(int id)
    {
        var view = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        view.name = $"Player_{id}";
        view.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        ViewMats.Set(view.GetComponent<Renderer>(), Color.cyan);
        return view;
    }

    private static void RefreshPlayerView(LogicPlayer p, Transform view)
    {
        view.localPosition = new Vector3(p.X.AsFloat, 0.25f, p.Y.AsFloat);
    }

    private static void DespawnView(Transform view)
    {
        Object.Destroy(view.gameObject);
    }

    private static long GenerateSeed()
    {
        return DateTime.UtcNow.Ticks;
    }
}
