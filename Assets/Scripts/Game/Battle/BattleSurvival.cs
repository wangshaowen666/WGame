/*--------------------------------------------------------------
 * File: BattleSurvival.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/26 14:49:35 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public enum Camp
{
    Self = 1,
    Ally = 2,
    Enemy = 4,
    Neutral = 8,
}

public class BattleSurvival : BattleBase
{
    private const int PlayerEid = 10000;
    
    public PlayerPlane PlayerPlane => _playerPlane;
    
    private PlayerPlane _playerPlane;

    private Transform _worldRoot;
    private Transform _aircraftRoot;
    public Transform BulletRoot;
    private Transform _effectRoot;
    
    public override void Init()
    {
        base.Init();
        InitWorldRoot();
        CreatePlayerPlane(PlayerEid);
    }

    private void CreatePlayerPlane(int eId)
    {
        EntityMgr.Instance.CreateEntity(eId, _aircraftRoot, OnLoadShipFinish);
    }

    private void OnLoadShipFinish(EntityBase entity)
    {
        _playerPlane = entity as PlayerPlane;
        if (_playerPlane != null) 
            _playerPlane.OnInit(PlayerEid, Camp.Self, this);
    }
    
    private void InitWorldRoot()
    {
        _worldRoot = new GameObject("World").transform;

        _aircraftRoot = CreateChildRoot("Aircraft");
        BulletRoot = CreateChildRoot("Bullet");
        _effectRoot = CreateChildRoot("Effect");
    }

    private Transform CreateChildRoot(string childName)
    {
        var trans = new GameObject(childName).transform;
        trans.SetParent(_worldRoot, false);
        
        return trans;
    }
}