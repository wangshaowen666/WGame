/*--------------------------------------------------------------
 * File: EffectEntity.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
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
        var time = GameMgr.DataTable.TbEffect[id].Time;
        if (time > 0)
        {
            FrameworkMgr.Timer.StartDelay((int)(time * 1000), OnFinish);
        }
    }

    private void OnFinish(int _)
    {
        GameMgr.Entity.RecycleEntity(_id, this);
    }
}