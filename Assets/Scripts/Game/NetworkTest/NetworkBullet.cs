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
    public float fireRate = 0.2f;

    public NetworkVariable<float> fireCooldown = new NetworkVariable<float>(0f);

    private void Awake()
    {
        fireCooldown.Value -= fireRate;
    }
    
    

}