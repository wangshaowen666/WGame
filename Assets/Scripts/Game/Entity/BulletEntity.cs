/*--------------------------------------------------------------
 * File: BulletEntity.cs
 * Author: Wang ShaoWen
 * Time: 2026/05/11 11:53:55 
 *--------------------------------------------------------------
 */

using System.Threading;
using UnityEngine;

public class BulletEntity : EntityBase
{
    private const int MoveTime = 3;
    
    private EntityBase _owner;
    private int _speed;
    private int _eid;
    private float _timer;

    private CancellationTokenSource _cancel;

    public void OnInit(int id, Camp camp, int speed, float angle, EntityStatsBase owner)
    {
        base.OnInit(id);

        _eid = id;
        _speed = speed;
        transform.eulerAngles = new Vector3(0, angle, 0);
        _cancel = Timer.StartRepeat(20, OnMove, 150, true);
    }

    private void OnMove(int i)
    {
        transform.position += transform.forward * (_speed * 0.02f);
        if (i == 150)
        {
            EntityMgr.Instance.RecycleEntity(_eid, this);
        }
    }

    public void MyUpdate(float deltaTime, float realDeltaTime)
    {
        transform.position += transform.forward * (_speed * deltaTime);
        _timer += deltaTime;
        if (_timer >= MoveTime)
        {
            EntityMgr.Instance.RecycleEntity(_eid, this);
        }
    }

    public override void OnRecycle()
    {
        base.OnRecycle();
        if (_cancel != null)
        {
            _cancel.Cancel();
            _cancel = null;
        }
    }

    private void OnDestroy()
    {
        if (_cancel != null)
        {
            _cancel.Cancel();
            _cancel = null;
        }
    }
}