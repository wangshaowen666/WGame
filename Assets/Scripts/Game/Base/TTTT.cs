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

    // 路径旁预置塔位（覆盖蛇形路径，射程 3 格内），放塔菜单按此轮询放置
    private static readonly int[,] s_towerSlots = { { 1, 2 }, { 2, 7 }, { 4, 9 }, { 5, 13 }, { 2, 11 } };
    private int _slotIndex;

    private void Start()
    {
        // 失焦不暂停（切去看服务器终端时客户端继续收帧，否则会积压延迟）
        Application.runInBackground = true;
        
        //GameMgr.Init();
    }

    [ContextMenu("0.登录测试账号(HTTP)")]
    void LoginTestAccount()
    {
        const string user = "test";
        const string pass = "test";

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

    [ContextMenu("2.进入战斗")]
    void EnterBattle()
    {
        GameMgr.Battle.EnterBattle();
        UiLog("进入战斗");
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

    [ContextMenu("5.退出战斗")]
    void ExitBattle()
    {
        GameMgr.Battle.ExitBattle();
    }

    [ContextMenu("帧同步状态")]
    void FrameStatus()
    {
        Log.Info("当前帧:", GameMgr.FrameSync.CurFrameId, "缓冲:", GameMgr.FrameSync.BufferCount);
    }

    private bool IsInBattle()
    {
        if (GameMgr.Battle.CurrentBattle is BattleTD) return true;
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
        if (GUILayout.Button("2. 进入战斗", GetBtnStyle())) EnterBattle();
        if (GUILayout.Button("3. 放塔", GetBtnStyle())) PlaceTower();
        if (GUILayout.Button("4. 升级塔", GetBtnStyle())) UpgradeTower();
        if (GUILayout.Button("5. 退出战斗", GetBtnStyle())) ExitBattle();

        GUILayout.Space(10);
        GUILayout.Label(NetStateText(), GetLabelStyle());
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

    private string BattleStateText()
    {
        if (GameMgr.Battle.CurrentBattle is BattleTD td)
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
