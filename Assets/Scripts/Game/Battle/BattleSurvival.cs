/*--------------------------------------------------------------
 * File: BattleSurvival.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/26 14:49:35 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class BattleSurvival : BattleBase
{
    private const int PlayerEid = 10000;
    
    private PlayerShip _playerShip;

    private Transform _worldRoot;
    private Transform _aircraftRoot;
    private Transform _bulletRoot;
    private Transform _effectRoot;
    
    public override void Init()
    {
        base.Init();
        InitWorldRoot();
        CreatePlayerShip(PlayerEid);
    }

    private void CreatePlayerShip(int eId)
    {
        EntityMgr.Instance.ShowEntity(eId, _aircraftRoot, OnLoadShipFinish);
    }

    private void OnLoadShipFinish(EntityBase entity)
    {
        _playerShip = entity as PlayerShip;
        if (_playerShip != null) 
            _playerShip.OnInit(PlayerEid);
    }
    
    private void InitWorldRoot()
    {
        _worldRoot = new GameObject("World").transform;

        _aircraftRoot = CreateChildRoot("Aircraft");
        _bulletRoot = CreateChildRoot("Bullet");
        _effectRoot = CreateChildRoot("Effect");
    }

    private Transform CreateChildRoot(string childName)
    {
        var trans = new GameObject(childName).transform;
        trans.SetParent(_worldRoot, false);
        
        return trans;
    }
}