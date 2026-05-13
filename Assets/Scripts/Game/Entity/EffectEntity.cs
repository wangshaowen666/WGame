/*--------------------------------------------------------------
 * File: EffectEntity.cs
 * Author: Wang ShaoWen
 * Time: 2026/05/11 16:36:15 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class EffectEntity : EntityBase
{
    public override void OnInit(int id)
    {
        base.OnInit(id);
        var time = DataTableMgr.Instance.TbEffect[id].Time;
        if (time > 0)
        {
            Timer.StartDelay((int)(time * 1000), OnFinish);
        }
    }

    private void OnFinish(int _)
    {
        EntityMgr.Instance.RecycleEntity(_id, this);
    }
}