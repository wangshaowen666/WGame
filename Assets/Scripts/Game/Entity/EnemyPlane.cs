/*--------------------------------------------------------------
 * File: EnemyPlane.cs
 * Author: Wang ShaoWen
 * Time: 2026/05/13 17:06:04 
 *--------------------------------------------------------------
 */

using System;
using System.Threading;
using cfg;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyPlane : EntityBase, IUpdateable
{
    private const float XSpeedMul = 2.5f;
    private const float ZSpeedMul = 0.8f;

    public PlaneStats Stats => _stats;

    private BattleSurvival _battle;
    private DPlane _cfg;
    private PlaneStats _stats;
    private CancellationTokenSource _attackCts;
    private Camera _camera;
    private Transform _shootTrans;
    private int _eid;
    private float _xDir;

    public void OnInit(int id, Camp camp, BattleBase battle)
    {
        base.OnInit(id);
        _eid = id;
        _cfg = DataTableMgr.Instance.TbPlane[id];
        _battle = battle as BattleSurvival;
        _stats = ClassPool.Get<PlaneStats>();
        _stats.Init(_cfg);
        _stats.OnDeath += OnDeath;
        _stats.OnHpChanged += OnHpChanged;
        _camera = Camera.main;
        _shootTrans = transform.Find("Weapon Point");
        Camp = camp;
        _xDir = Random.value > 0.5f ? 1f : -1f;
        AutoAttack();

        CreateEngine();
    }
    
    private void CreateEngine()
    {
        var root = transform.Find("Thruster Point");
        EntityMgr.Instance.CreateEntity(_cfg.ThrusterId, root, null);
    }

    public void MyUpdate(float deltaTime, float realDeltaTime)
    {
        if (_stats.IsDead)
            return;

        float speed = _stats.ModifiedSpeed;
        Vector3 pos = transform.position;

        pos.x += speed * XSpeedMul * _xDir * deltaTime;

        PlayerPlane player = _battle.PlayerPlane;
        if (player != null)
        {
            float dz = player.transform.position.z - pos.z;
            pos.z += Mathf.Sign(dz) * Mathf.Min(Mathf.Abs(dz), speed * ZSpeedMul * deltaTime);
        }

        if (_camera != null)
        {
            Vector3 vp = _camera.WorldToViewportPoint(pos);
            if (vp.x <= 0.05f || vp.x >= 0.95f)
            {
                _xDir = -_xDir;
                vp.x = Mathf.Clamp(vp.x, 0.05f, 0.95f);
            }
            pos = _camera.ViewportToWorldPoint(vp);
        }

        transform.position = pos;
    }

    private void AutoAttack()
    {
        int ms = (int)(_cfg.Interval * 1000);
        _attackCts = Timer.StartRepeat(ms, OnAttack);
    }

    private void OnAttack(int count)
    {
        if (_stats.IsDead)
            return;

        EntityMgr.Instance.CreateEntity(_cfg.BulletId, _battle.BulletRoot, OnLoadBulletFinish);
    }

    private void OnLoadBulletFinish(EntityBase entity)
    {
        BulletEntity bullet = entity as BulletEntity;
        if (bullet != null)
        {
            bullet.transform.position = _shootTrans.position;
            bullet.OnInit(_cfg.BulletId, Camp.Enemy, _cfg.BulletSpeed, 180f, _stats.GetAttackDamage());
        }
    }

    private void OnDeath()
    {
        _attackCts?.Cancel();
        EntityMgr.Instance.CreateEntity(_cfg.DeadEffId, transform, null);
        Timer.StartDelay(200, _ => EntityMgr.Instance.RecycleEntity(_eid, this));
    }

    private void OnHpChanged(int damage, int currentHp)
    {
        //Log.Info($"EnemyPlane 受伤: {damage}, 剩余HP: {currentHp}");
    }

    public override void OnRecycle()
    {
        base.OnRecycle();
        CancelAttack();
    }

    private void OnDestroy()
    {
        CancelAttack();
        if (_stats != null)
        {
            _stats.OnDeath -= OnDeath;
            _stats.OnHpChanged -= OnHpChanged;
            ClassPool.Recycle(_stats);
            _stats = null;
        }
    }

    private void CancelAttack()
    {
        if (_attackCts != null)
        {
            _attackCts.Cancel();
            _attackCts.Dispose();
            _attackCts = null;
        }
    }
}