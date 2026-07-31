/*--------------------------------------------------------------
 * File: NetworkBullet.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/29 16:55:38 
 *--------------------------------------------------------------
 */

using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkBullet : NetworkBehaviour
{
    private ObjectPool<GameObject> _bulletPool;
    private int _moveCount;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            _bulletPool = FrameworkMgr.ObjectPool.GetPool<GameObject>();
            _moveCount = 0;
            CoreMgr.Timer.StartRepeat(20, OnMove, 100, true);
        }
    }

    public void ReStart()
    {
        if (IsServer)
        {
            _moveCount = 0;
            CoreMgr.Timer.StartRepeat(20, OnMove, 100, true);
        }
    }

    private void OnMove()
    {
        if (IsServer)
        {
            _moveCount++;
            transform.Translate(transform.forward * Time.deltaTime * 30, Space.World);
            if (_moveCount == 100)
            {
                _bulletPool.PutObj("bullet", gameObject);
            }
        }
    }
}