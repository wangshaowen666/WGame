/*--------------------------------------------------------------
 * File: TTTT.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/12 18:09:09
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TTTT : MonoBehaviour
{
    /// <summary>游戏服务器地址：真机/双端测试用局域网 IP（本机 WiFi en1，192.168.14.x 网段），本地单机可改回 127.0.0.1</summary>
    private const string ServerIp = "192.168.14.200";
    private const int ServerPort = 7777;

    private int _lastTowerX = 1; // 记录最后一次放塔位置，供"升级塔"菜单使用
    private int _lastTowerY = 2;

    private int _knownRoomId;      // 已知房间号（本端建房成功自动记录，也可 UI 手动输入）
    private string _roomInput = ""; // UI 房间号输入框内容

    /// <summary>登录测试账号（Inspector 可改：Editor 双端分别填不同账号；真机打包前填好）</summary>
    public string TestUsername = "wsw";
    public string TestPassword = "123456";

    // 路径旁预置塔位（覆盖蛇形路径，射程 3 格内），放塔菜单按此轮询放置
    private static readonly int[,] s_towerSlots = { { 1, 2 }, { 2, 7 }, { 4, 9 }, { 5, 13 }, { 2, 11 } };
    private int _slotIndex;

    private void Start()
    {
        // 失焦不暂停（切去看服务器终端时客户端继续收帧，否则会积压延迟）
        Application.runInBackground = true;

        //GameMgr.Init();

        // 建房/加房成功自动记录房间号（供 ContextMenu 快捷加入）
        GameMgr.Room.OnRoomResp += OnRoomResp;

        // 全员就绪收到开战推送 -> 自动进入战斗（仅联机测试场景：帧同步塔防走此路径；
        // 单机吸血鬼幸存者用下方"进入吸血鬼单机战斗"入口，不走房间/联机）
        GameMgr.Room.OnStartGamePush += _ =>
        {
            GameMgr.Battle.EnterBattle(BattleMode.TowerDefense);
            UiLog("全员就绪，开战!");
        };

        // 游戏结束推送 -> 战报提示（回调内 StartFrame 尚未清零，可算时长/波次）
        GameMgr.Room.OnGameEndPush += push =>
        {
            var frames = push.EndFrame - GameMgr.Room.StartFrame;
            var secs = frames * FrameSyncMgr.LogicFrameMs / 1000;
            var waves = frames / TdLogic.SpawnEveryFrames;
            UiLog($"游戏结束({push.Result})! 坚持{secs}秒/{waves}波, 奖励{push.GoldReward}金币(服务器结算), 可再就绪重开");
        };
    }

    [ContextMenu("0.登录测试账号(HTTP)")]
    void LoginTestAccount()
    {
        var user = TestUsername;
        var pass = TestPassword;

        GameMgr.Account.Login(user, pass, resp =>
        {
            if (resp.ErrorCode == NetMsg.ErrorCode.ErrorNone)
            {
                UiLog($"HTTP 登录成功: {user}, playerId={GameMgr.Account.PlayerId}");
                return;
            }

            // 账号不存在 -> 注册后重登（已存在等其他错误直接报）
            if (resp.ErrorCode == NetMsg.ErrorCode.ErrorWrongCredentials)
            {
                GameMgr.Account.Register(user, pass, regResp =>
                {
                    if (regResp.ErrorCode == NetMsg.ErrorCode.ErrorNone)
                        GameMgr.Account.Login(user, pass, _ => UiLog($"注册并登录成功: {user}, playerId={GameMgr.Account.PlayerId}"));
                    else
                        UiLog($"注册失败: {regResp.ErrorCode}");
                });
            }
            else
            {
                UiLog($"HTTP 登录失败: {resp.ErrorCode}");
            }
        });
        UiLog($"HTTP 登录请求已发送（{GameConfig.LoginServerUrl}）");
    }

    [ContextMenu("1.连接服务器")]
    void ConnectServer()
    {
        CoreMgr.Net.Connect(ServerIp, ServerPort);
        UiLog($"连接 {ServerIp}:{ServerPort} ...");
    }

    [ContextMenu("2.就绪(全员就绪自动开战)")]
    void ReadyForBattle()
    {
        if (!GameMgr.Room.IsInRoom)
        {
            UiLog("未在房间内，请先建房/匹配/加入");
            Log.Warning("未在房间内");
            return;
        }
        GameMgr.Room.Ready();
        UiLog("就绪请求已发送，等待全员就绪...");
    }

    [ContextMenu("1.5创建房间")]
    void CreateRoom()
    {
        GameMgr.Room.CreateRoom();
        UiLog("创建房间请求已发送");
    }

    /// <summary>建房/加房成功回调入口（UI 按钮用）：记录房间号</summary>
    private void OnRoomResp(NetMsg.RoomResp resp)
    {
        if (resp.ErrorCode == NetMsg.ErrorCode.ErrorNone)
            _knownRoomId = resp.RoomId;
    }

    [ContextMenu("1.6加入上次房间号")]
    void JoinRoom()
    {
        if (_knownRoomId == 0)
        {
            UiLog("没有已知房间号（先在本端或另一端建房后记下房间号，或用 UI 输入框输入）");
            return;
        }
        GameMgr.Room.JoinRoom(_knownRoomId);
        UiLog($"加入房间 {_knownRoomId} 请求已发送");
    }

    [ContextMenu("1.7退出房间")]
    void LeaveRoom()
    {
        GameMgr.Room.LeaveRoom();
        UiLog("退出房间请求已发送");
    }

    [ContextMenu("3.放塔(路径旁预置位)")]
    void PlaceTower()
    {
        if (!IsInBattle()) return;
        _lastTowerX = s_towerSlots[_slotIndex, 0];
        _lastTowerY = s_towerSlots[_slotIndex, 1];
        _slotIndex = (_slotIndex + 1) % s_towerSlots.GetLength(0);
        GameMgr.FrameSync.SendInput(1, _lastTowerX, _lastTowerY);
        LogBattleState("[操作] 放塔:", _lastTowerX, ",", _lastTowerY);
    }

    [ContextMenu("4.升级塔(上次位置)")]
    void UpgradeTower()
    {
        if (!IsInBattle()) return;
        GameMgr.FrameSync.SendInput(2, _lastTowerX, _lastTowerY);
        LogBattleState("[操作] 升级塔:", _lastTowerX, ",", _lastTowerY);
    }

    [ContextMenu("7.进入吸血鬼单机战斗(骨架)")]
    void EnterVampireBattle()
    {
        // 临时入口（阶段 1-2 骨架验证）：单机跳过 Room.StartFrame 校验，直接进入
        GameMgr.Battle.EnterBattle(BattleMode.VampireSurvivor);
        UiLog("进入吸血鬼幸存者单机战斗(骨架)");
    }

    [ContextMenu("8.性能基线测试(300物体)")]
    void PerfBaseline()
    {
        // 临时入口（阶段 1-1）：300 池化 GameObject 每帧位移测帧率，见 PerfBaselineTest
        if (FindObjectOfType<PerfBaselineTest>() == null)
            new GameObject("PerfBaseline", typeof(PerfBaselineTest));
        else
            UiLog("性能基线测试已存在");
        UiLog("性能基线测试启动(300物体)");
    }

    [ContextMenu("6.查询金币(HTTP)")]
    void QueryGold()
    {
        GameMgr.PlayerData.Load(resp =>
        {
            if (resp.ErrorCode == NetMsg.ErrorCode.ErrorNone)
                UiLog($"当前金币: {GameMgr.PlayerData.Gold} (通关进度 {GameMgr.PlayerData.StageProgress})");
            else
                UiLog($"查询养成数据失败: {resp.ErrorCode}（需先登录）");
        });
        UiLog("查询养成数据请求已发送");
    }

    [ContextMenu("帧同步状态")]
    void FrameStatus()
    {
        Log.Info("当前帧:", GameMgr.FrameSync.CurFrameId, "缓冲:", GameMgr.FrameSync.BufferCount);
    }

    private bool IsInBattle()
    {
        if (GameMgr.Battle.CurrentBattleView is TdView) return true;
        Log.Warning("未进入战斗，请先执行 2.进入战斗");
        UiLog("未进入战斗，请先点 2.进入战斗");
        return false;
    }

    /// <summary>操作后打印战斗状态（金币/塔数/敌数，方便确认操作是否生效）</summary>
    private void LogBattleState(params object[] msg)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var m in msg) sb.Append(m);
        sb.Append(" -> ").Append(BattleStateText());
        Log.Info(sb.ToString());
        UiLog(sb.ToString());
    }

    // ---------------- 临时测试 UI（IMGUI，手机真机可点，无需 prefab） ----------------

    private bool _uiVisible = true; // 面板显隐（双端验证时可收起，避免遮挡战场）
    private Vector2 _statusScroll;
    private readonly List<string> _uiLogs = new(); // 最近几条操作回显（真机没 Console，靠这里看结果）

    private void OnGUI()
    {
        if (!_uiVisible)
        {
            // 收起状态只留一个小开关
            if (GUI.Button(new Rect(10, 10, 90, 50), "测试UI"))
                _uiVisible = true;
            return;
        }

        // 左侧按钮列（竖屏左半边），右侧状态栏
        var area = new Rect(10, 10, Screen.width * 0.42f, Screen.height - 20);
        GUILayout.BeginArea(area, GetPanelStyle());
        GUILayout.Label("帧同步测试", GetLabelStyle());

        if (GUILayout.Button("0. 登录测试账号", GetBtnStyle())) LoginTestAccount();
        if (GUILayout.Button("1. 连接服务器", GetBtnStyle())) ConnectServer();

        // 房间操作：建房 / 输入房间号加入 / 退房
        if (GUILayout.Button("1.5 创建房间", GetBtnStyle())) CreateRoom();
        GUILayout.BeginHorizontal();
        _roomInput = GUILayout.TextField(_roomInput, 4, GetBtnStyle());
        if (GUILayout.Button("加入", GetBtnStyle(), GUILayout.Width(100)))
        {
            if (int.TryParse(_roomInput, out var rid))
            {
                _knownRoomId = rid;
                GameMgr.Room.JoinRoom(rid);
                UiLog($"加入房间 {rid} 请求已发送");
            }
            else
                UiLog("请输入数字房间号");
        }
        GUILayout.EndHorizontal();

        // 匹配：开始（已在房/匹配中则显示取消）
        if (GameMgr.Room.IsMatching)
        {
            if (GUILayout.Button("取消匹配(匹配中...)", GetBtnStyle()))
            {
                GameMgr.Room.CancelMatch();
                UiLog("取消匹配请求已发送");
            }
        }
        else
        {
            if (GUILayout.Button("1.6 开始匹配", GetBtnStyle()))
            {
                GameMgr.Room.StartMatch();
                UiLog("匹配请求已发送");
            }
        }

        if (GUILayout.Button("1.7 退出房间", GetBtnStyle())) LeaveRoom();

        if (GUILayout.Button("2. 就绪(自动开战)", GetBtnStyle())) ReadyForBattle();
        if (GUILayout.Button("3. 放塔", GetBtnStyle())) PlaceTower();
        if (GUILayout.Button("4. 升级塔", GetBtnStyle())) UpgradeTower();
        if (GUILayout.Button("7. 吸血鬼单机(骨架)", GetBtnStyle())) EnterVampireBattle();
        if (GUILayout.Button("8. 性能基线测试", GetBtnStyle())) PerfBaseline();
        if (GUILayout.Button("6. 查询金币", GetBtnStyle())) QueryGold();

        GUILayout.Space(10);
        GUILayout.Label(NetStateText(), GetLabelStyle());
        GUILayout.Label(RoomStateText(), GetLabelStyle());
        GUILayout.Label(BattleStateText(), GetLabelStyle());

        GUILayout.Space(10);
        GUILayout.Label("--- 操作回显 ---", GetLabelStyle());
        _statusScroll = GUILayout.BeginScrollView(_statusScroll, GUILayout.Height(200));
        for (int i = _uiLogs.Count - 1; i >= 0; i--)
            GUILayout.Label(_uiLogs[i], GetLabelStyle());
        GUILayout.EndScrollView();

        if (GUILayout.Button("收起", GetBtnStyle())) _uiVisible = false;
        GUILayout.EndArea();
    }

    private string NetStateText()
    {
        var auth = GameMgr.NetMsg.IsUdpAuthed
            ? $"已认证(P{GameMgr.NetMsg.UdpPlayerId})"
            : (CoreMgr.Net.IsConnected ? "未认证" : "-");
        return $"网络:{(CoreMgr.Net.IsConnected ? "已连接" : "未连接")} {auth} 帧:{GameMgr.FrameSync.CurFrameId} 缓冲:{GameMgr.FrameSync.BufferCount}";
    }

    private string RoomStateText()
    {
        if (GameMgr.Room.IsMatching) return "房间: 无 (匹配中...)";
        if (!GameMgr.Room.IsInRoom) return "房间: 无";
        var ready = GameMgr.Room.IsReady ? " 已就绪" : "";
        var over = GameMgr.Room.IsBattleOver ? " [战斗结束,可再就绪]" : "";
        return $"房间: {GameMgr.Room.CurRoomId} 成员: {string.Join(",", GameMgr.Room.MemberIds)}{ready}{over}";
    }

    private string BattleStateText()
    {
        if (GameMgr.Battle.CurrentBattleView is TdView td)
        {
            var reject = td.RejectText;
            return td.GetDebugState() + (string.IsNullOrEmpty(reject) ? "" : $"\n被拒: {reject}");
        }
        return "战斗: 未进入";
    }

    /// <summary>真机回显日志（替代 Console 查看）</summary>
    private void UiLog(string msg)
    {
        _uiLogs.Add(msg);
        if (_uiLogs.Count > 20) _uiLogs.RemoveAt(0);
    }

    private GUIStyle _btnStyle;
    private GUIStyle GetBtnStyle()
    {
        if (_btnStyle == null)
        {
            _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 32, fixedHeight = 70 };
        }
        return _btnStyle;
    }

    private GUIStyle _labelStyle;
    private GUIStyle GetLabelStyle()
    {
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                wordWrap = true,
                normal = { textColor = Color.white },
            };
        }
        return _labelStyle;
    }

    private GUIStyle _panelStyle;
    private GUIStyle GetPanelStyle()
    {
        if (_panelStyle == null)
        {
            _panelStyle = new GUIStyle(GUI.skin.box);
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            tex.Apply();
            _panelStyle.normal.background = tex;
        }
        return _panelStyle;
    }
}
