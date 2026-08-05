/*--------------------------------------------------------------
 * File: NetworkPlayer.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/27 19:06:53 
 *--------------------------------------------------------------
 */

using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer LocalPlayer;
   
    //private NetworkVariable<float> fireCooldown = new NetworkVariable<float>(0f);

    private ObjectPool<GameObject> objectPool;
    private float _fireRate = 0.2f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsLocalPlayer)
        {
            LocalPlayer = this;
        }
        
        if (IsServer)
        {
            //fireCooldown.Value -= _fireRate;
            objectPool = CoreMgr.ObjectPool.RegisterPool<GameObject>();
        }
    }

    public void Fire()
    {
        FireServerRpc();
    }

    public void Move(Vector3 dir)
    {
        MoveServerRpc(dir);
    }

    [ServerRpc]
    private void MoveServerRpc(Vector3 dir)
    {
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = targetRot;
        transform.position += dir;
        OnMoveClientRpc();
    }

    [ServerRpc]
    private void FireServerRpc()
    {
        // if (Time.time - fireCooldown.Value < _fireRate)
        // {
        //     return;
        // }
        
        //fireCooldown.Value = Time.time;
        var bullet = objectPool.GetObj("bullet");
        if (bullet == null)
        {
            CoreMgr.Res.LoadAsync<GameObject>("PlayerBullet", OnLoadBulletFinish);
        }
        else
        {
            bullet.transform.position = transform.position;
            bullet.transform.rotation = transform.rotation;
            var netBullet = bullet.GetComponent<NetworkBullet>();
            netBullet.ReStart();
        }
    }

    [ClientRpc]
    private void OnMoveClientRpc()
    {
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