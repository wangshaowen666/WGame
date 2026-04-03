/*--------------------------------------------------------------
 * File: NetworkBullet.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/29 16:55:38 
 *--------------------------------------------------------------
 */

using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkBullet : NetworkBehaviour
{
    private ObjectPool<GameObject> _bulletPool;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            _bulletPool = ObjectMgr.Instance.GetPool<GameObject>();
            Timer.StartRepeat(20, OnMove, 100, true);
        }
    }

    public void ReStart()
    {
        if (IsServer)
        {
            Timer.StartRepeat(20, OnMove, 100, true);
        }
    }

    private void OnMove(int flag)
    {
        if (IsServer)
        {
            transform.Translate(transform.forward * Time.deltaTime * 30, Space.World);
            if (flag == 100)
            {
                _bulletPool.PutObj("bullet", gameObject);
            }
        }
    }
}