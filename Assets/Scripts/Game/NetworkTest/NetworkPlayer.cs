/*--------------------------------------------------------------
 * File: NetworkPlayer.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/27 19:06:53 
 *--------------------------------------------------------------
 */

using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer LocalPlayer;
   
    [HideInInspector]
    public NetworkVariable<float> fireCooldown = new NetworkVariable<float>(0f);
    
    private ObjectPool<GameObject> objectPool = new ObjectPool<GameObject>();
    private float _fireRate = 0.2f;

    private void Awake()
    {
        fireCooldown.Value -= _fireRate;
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Log.Info(1111);
        if (IsLocalPlayer)
        {
            Log.Info(2222);
            LocalPlayer = this;
        }
    }

    public void Fire()
    {
        FireServerRpc();
    }

    public void Move()
    {
        MoveServerRpc();
    }

    [ServerRpc]
    private void MoveServerRpc()
    {
        transform.position += Vector3.up;
        OnMoveClientRpc();
    }

    [ServerRpc]
    private void FireServerRpc()
    {
        if (Time.time - fireCooldown.Value < _fireRate)
        {
            return;
        }
        
        fireCooldown.Value = Time.time;
        var bullet = objectPool.GetObj("bullet");
        if (bullet == null)
        {
            ResMgr.Instance.LoadAsync<GameObject>("PlayerBullet", OnLoadBulletFinish);
        }
        else
        {
            bullet.transform.position = transform.position;
            bullet.transform.rotation = transform.rotation;
            var netObj = bullet.GetComponent<NetworkObject>();
            netObj.Spawn();
        }
    }

    [ClientRpc]
    private void OnMoveClientRpc()
    {
        Log.Info("客户端播放移动特效：" + transform.position);
    }
    

    [ServerRpc]
    public void TakeDamageServerRpc(int damage)
    {
        Log.Info("服务器执行逻辑");
        
        ShowDamageTipClientRpc(damage);
    }

    [ClientRpc]
    private void ShowDamageTipClientRpc(int damage)
    {
        
    }
    
    private void OnLoadBulletFinish(GameObject asset, object userdata)
    {
        var bullet = Instantiate(asset, transform.position, transform.rotation);
        var netObj = bullet.GetComponent<NetworkObject>();
        netObj.Spawn();
    }
    
}