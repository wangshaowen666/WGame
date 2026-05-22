/*--------------------------------------------------------------
 * File: TestBattlePanel.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/28 18:54:36 
 *--------------------------------------------------------------
 */

using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TestBattlePanel : UIPanelBase
{
    [SerializeField]
    private Button _upBtn;
    [SerializeField]
    private Button _downBtn;
    [SerializeField]
    private Button _leftBtn;
    [SerializeField]
    private Button _rightBtn;
    [SerializeField]
    private Button _fireBtn;
    [SerializeField]
    private Button _clientBtn;
    [SerializeField]
    private Button _hostBtn;
    
    private float _fireRate = 0.2f;
    private float _curFireTime;
    
    private NetworkManager _networkManager;
    
    private void Awake()
    {
        _upBtn.onClick.AddListener(ClickUp);
        _downBtn.onClick.AddListener(ClickDown);
        _leftBtn.onClick.AddListener(ClickLeft);
        _rightBtn.onClick.AddListener(ClickRight);
        _fireBtn.onClick.AddListener(ClickFire);
        _clientBtn.onClick.AddListener(ClickClient);
        _hostBtn.onClick.AddListener(ClickHost);

        _curFireTime = -_fireRate;
        _networkManager = FindObjectOfType<NetworkManager>();
    }


    private void ClickHost()
    {
        // 手动初始化UnityTransport
        if (_networkManager.NetworkConfig.NetworkTransport is UnityTransport transport)
        {
            transport.Initialize();
        }
        // 启动Host
        var result = _networkManager.StartHost();
        Debug.Log($"Host启动结果: {result}"); // 必须是true
    }

    private void ClickClient()
    {
        if (_networkManager.IsClient || _networkManager.IsServer)
        {
            Debug.LogWarning("已处于网络运行状态，无需重复启动Client！");
            return;
        }

        // 关键修复2：确认NetworkManager和Transport有效
        if (_networkManager == null)
        {
            Debug.LogError("NetworkManager未赋值！");
            return;
        }

        // 关键修复3：设置服务器IP和端口（必须和IDE的Host一致）
        if (_networkManager.NetworkConfig.NetworkTransport is UnityTransport transport)
        {
            transport.SetConnectionData(
                "192.168.3.72",  //
                (ushort)7766      // 端口和NetworkManager设置一致（7777）
            );
            Debug.Log($"正在连接服务器：192.168.3.72:7777");
        }
        else
        {
            Debug.LogError("未找到UnityTransport组件！");
            return;
        }

        // 启动Client（带结果检查）
        var startResult = _networkManager.StartClient();
        if (startResult)
        {
            Debug.Log("安卓Client启动成功，等待连接...");
        }
        else
        {
            Debug.LogError("安卓Client启动失败！");
        }
    }
    
    private void ClickRight()
    {
        NetworkPlayer.LocalPlayer.Move(Vector3.right);
    }

    private void ClickLeft()
    {
        NetworkPlayer.LocalPlayer.Move(Vector3.left);
    }

    private void ClickDown()
    {
        NetworkPlayer.LocalPlayer.Move(Vector3.back);
    }

    private void ClickUp()
    {
        NetworkPlayer.LocalPlayer.Move(Vector3.forward);
    }

    private void ClickFire()
    {
        if (Time.time - _curFireTime >= _fireRate)
        {
            NetworkPlayer.LocalPlayer.Fire();
            _curFireTime = Time.time;
        }
    }
}