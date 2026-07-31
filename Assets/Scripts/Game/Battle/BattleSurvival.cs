/*--------------------------------------------------------------
 * File: BattleSurvival.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/26 14:49:35 
 *--------------------------------------------------------------
 */

using System;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

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
    private const int EnemyEid = 10001;
    private const int SpawnIntervalMs = 5000;

    public PlayerPlane PlayerPlane => _playerPlane;

    private PlayerPlane _playerPlane;
    private Camera _camera;
    private CancellationTokenSource _spawnCts;

    private Transform _worldRoot;
    private Transform _aircraftRoot;
    public Transform BulletRoot;
    private Transform _effectRoot;

    public override void Init()
    {
        base.Init();
        _camera = Camera.main;
        InitWorldRoot();
        CreatePlayerPlane(PlayerEid);
        StartSpawnEnemy();
    }

    private void CreatePlayerPlane(int eId)
    {
        GameMgr.Entity.CreateEntity(eId, _aircraftRoot, OnLoadPlayerFinish);
    }

    private void OnLoadPlayerFinish(EntityBase entity)
    {
        _playerPlane = entity as PlayerPlane;
        if (_playerPlane != null)
            _playerPlane.OnInit(PlayerEid, Camp.Self, this);
    }

    private void StartSpawnEnemy()
    {
        _spawnCts = CoreMgr.Timer.StartRepeat(SpawnIntervalMs, () => SpawnEnemy(), -1, true);
    }

    private void SpawnEnemy()
    {
        if (_camera == null) return;

        float x = Random.Range(0.1f, 0.9f);
        Vector3 spawnVp = new Vector3(x, 0.95f, 0);
        Vector3 worldPos = _camera.ViewportToWorldPoint(spawnVp);
        Vector3 spawnPos = new Vector3(worldPos.x, 0, worldPos.z);
        GameMgr.Entity.CreateEntity(EnemyEid, _aircraftRoot, entity =>
        {
            EnemyPlane enemy = entity as EnemyPlane;
            if (enemy != null)
            {
                enemy.transform.position = spawnPos;
                enemy.OnInit(EnemyEid, Camp.Enemy, this);
            }
        });
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