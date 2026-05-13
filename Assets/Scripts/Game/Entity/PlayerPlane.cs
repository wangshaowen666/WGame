/*--------------------------------------------------------------
 * File: PlayerShip.cs
 * Author: Wang ShaoWen
 * Time: 2026/05/11 11:41:30 
 *--------------------------------------------------------------
 */

using System;
using System.Threading;
using cfg;
using UnityEngine;

public class PlayerPlane : EntityBase, IUpdateable
{
    public PlaneStats Stats => _stats;

    private const int BulletNum = 5;
    private const int BulletAngle = 10;
    
    private BattleSurvival _battle;
    private DPlane _cfg;
    private CancellationTokenSource _cancel;
    private Transform _shootTrans;
    private PlaneStats _stats;
    private Vector3 _moveTarget;
    private bool _hasMoveTarget;
    private Camera _camera;
    private Camp _camp;

    private int _bulletCount;
    private float _intervalAngle;
    
    public void OnInit(int id, Camp camp, BattleBase battle)
    {
        base.OnInit(id);
        _cfg = DataTableMgr.Instance.TbPlane[id];
        _battle = battle as BattleSurvival;
        _stats = ClassPool.Get<PlaneStats>();
        _stats.Init(_cfg);
        _stats.OnDeath += OnDeath;
        _stats.OnHpChanged += OnHpChanged;
        _camera = Camera.main;
        _camp = camp;
        
        _shootTrans = transform.Find("Weapon Point");
        _moveTarget = transform.position;

        _intervalAngle = (BulletNum - 1) / 2f;
        _intervalAngle = BulletAngle / _intervalAngle;
        
        CreateEngine();
        AutoAttack();
    }

    public void SetMoveTarget(Vector3 worldTarget)
    {
        _moveTarget = worldTarget;
        _hasMoveTarget = true;
    }

    private Vector3 ClampToViewport(Vector3 worldPos)
    {
        if (_camera == null)
            return worldPos;

        Vector3 viewportPos = _camera.WorldToViewportPoint(worldPos);
        viewportPos.x = Mathf.Clamp(viewportPos.x, 0.05f, 0.95f);
        viewportPos.y = Mathf.Clamp(viewportPos.y, 0.05f, 0.95f);
        return _camera.ViewportToWorldPoint(viewportPos);
    }

    private void CreateEngine()
    {
        var root = transform.Find("Thruster Point");
        EntityMgr.Instance.CreateEntity(_cfg.ThrusterId, root, null);
    }

    private void AutoAttack()
    {
        int ms = (int)(_cfg.Interval * 1000);
        _cancel = Timer.StartRepeat(ms, OnAttack);
    }

    private void OnAttack(int i)
    {
        if (_stats.IsDead)
            return;
        
        for (int j = 0; j < BulletNum; j++)
        {
            EntityMgr.Instance.CreateEntity(_cfg.BulletId, _battle.BulletRoot, OnLoadBulletFinish);
        }
    }

    private void OnLoadBulletFinish(EntityBase entity)
    {
        BulletEntity bullet = entity as BulletEntity;
        if (bullet != null)
        {
            _bulletCount++;
            bullet.transform.position = _shootTrans.position;
            bullet.OnInit(_cfg.BulletId, _camp, _cfg.BulletSpeed, -BulletAngle + _intervalAngle * (_bulletCount % BulletNum), _stats);
        }
    }

    private void OnDeath()
    {
        _cancel?.Cancel();
        EntityMgr.Instance.CreateEntity(_cfg.DeadEffId, transform, null);
        Timer.StartDelay(2000, _ => EntityMgr.Instance.RecycleEntity(_id, this));
    }

    private void OnHpChanged(int damage, int currentHp)
    {
        Log.Info($"PlayerPlane 受伤: {damage}, 剩余HP: {currentHp}");
    }

    public void MyUpdate(float deltaTime, float realDeltaTime)
    {
        if (_stats.IsDead || !_hasMoveTarget)
            return;

        float speed = _stats.ModifiedSpeed;
        Vector3 currentPos = transform.position;
        Vector3 targetPos = ClampToViewport(_moveTarget);
        Vector3 newPos = Vector3.MoveTowards(currentPos, targetPos, speed * Time.deltaTime);
        transform.position = newPos;
    }
    
     private void OnDestroy()
     {
         if (_stats != null)
         {
             _stats.OnDeath -= OnDeath;
             _stats.OnHpChanged -= OnHpChanged;
             ClassPool.Recycle(_stats);
             _stats = null;
         }
         
         _cancel?.Cancel();
         _cancel?.Dispose();
         _cancel = null;
    }
}