/*--------------------------------------------------------------
 * File: BulletEntity.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/05/11 11:53:55 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class BulletEntity : EntityBase, IUpdateable
{
    private const float MoveTime = 3f;

    private int _damage;
    private int _speed;
    private int _eid;
    private float _timer;

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    public void OnInit(int id, Camp camp, int speed, float angle, int damage)
    {
        base.OnInit(id);
        _eid = id;
        _speed = speed;
        _damage = damage;
        _timer = 0f;
        Camp = camp;
        transform.eulerAngles = new Vector3(0, angle, 0);
        _collider.enabled = true;
    }

    public void MyUpdate(float deltaTime, float realDeltaTime)
    {
        transform.position += transform.forward * (_speed * deltaTime);
        _timer += deltaTime;
        if (_timer >= MoveTime)
            GameMgr.Entity.RecycleEntity(_eid, this);
    }

    private void OnTriggerEnter(Collider other)
    {
        var otherEntity = other.GetComponentInParent<EntityBase>();
        if (otherEntity == null || IsSameCamp(otherEntity))
            return;

        EntityStatsBase targetStats = GetEntityStats(otherEntity);
        if (targetStats == null || targetStats.IsDead)
            return;

        targetStats.TakeDamage(_damage);
        GameMgr.Entity.RecycleEntity(_eid, this);
    }

    private bool IsSameCamp(EntityBase other)
    {
        if (Camp == Camp.Neutral || other.Camp == Camp.Neutral)
            return false;

        return ((int)Camp & (int)other.Camp) != 0;
    }

    private static EntityStatsBase GetEntityStats(EntityBase entity)
    {
        if (entity is PlayerPlane player)
            return player.Stats;
        if (entity is EnemyPlane enemy)
            return enemy.Stats;
        return null;
    }

    public override void OnRecycle()
    {
        _collider.enabled = false;
        base.OnRecycle();
    }
}