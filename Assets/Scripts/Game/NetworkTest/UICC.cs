/*--------------------------------------------------------------
 * File: UICC.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/28 18:54:36 
 *--------------------------------------------------------------
 */

using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class UICC : MonoBehaviour
{
    [SerializeField]
    private Text _hpText;
    [SerializeField]
    private Button _button;
    [SerializeField]
    private Button _clientBtn;
    [SerializeField]
    private Button _hostBtn;

    
    private float _fireRate = 0.2f;
    
    [SerializeField] 
    private NetworkManager networkManager;
    
    private void Awake()
    {
        _hpText.text = $"血量: {100}";
        _button.onClick.AddListener(ClickBtn);
        _clientBtn.onClick.AddListener(ClickClient);
        _hostBtn.onClick.AddListener(ClickHost);
        
        EventMgr.Instance.Register<int>(GameEvent.UIPanelOn, OnFlush);
    }

    private void OnFlush(int v)
    {
        _hpText.text = $"血量: {v}";
    }


    private void ClickHost()
    {
        // 手动初始化UnityTransport
        if (networkManager.NetworkConfig.NetworkTransport is UnityTransport transport)
        {
            transport.Initialize();
        }
        // 启动Host
        var result = networkManager.StartHost();
        Debug.Log($"Host启动结果: {result}"); // 必须是true
    }

    private void ClickClient()
    {
        if (networkManager.IsClient || networkManager.IsServer)
        {
            Debug.LogWarning("已处于网络运行状态，无需重复启动Client！");
            return;
        }

        // 关键修复2：确认NetworkManager和Transport有效
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager未赋值！");
            return;
        }

        // 关键修复3：设置服务器IP和端口（必须和IDE的Host一致）
        if (networkManager.NetworkConfig.NetworkTransport is UnityTransport transport)
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
        var startResult = networkManager.StartClient();
        if (startResult)
        {
            Debug.Log("安卓Client启动成功，等待连接...");
        }
        else
        {
            Debug.LogError("安卓Client启动失败！");
        }
    }

    private void ClickBtn()
    {
        NetworkPlayer.LocalPlayer.Move();
    }

    private void ClickFire()
    {
        if (Time.time - NetworkPlayer.LocalPlayer.fireCooldown.Value >= _fireRate)
        {
            NetworkPlayer.LocalPlayer.Fire();
        }
    }
}