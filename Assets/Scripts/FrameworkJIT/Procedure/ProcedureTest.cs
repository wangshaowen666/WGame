/*--------------------------------------------------------------
 * File: ProcedureTest.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 17:41:22 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class ProcedureTest : ProcedureBase
{
    private float _time;
    
    public override void OnEnter()
    {
        base.OnEnter();
        Log.Info("进入Test流程");
    }

    public override void OnUpdate(float deltaTime, float realDeltaTime)
    {
        base.OnUpdate(deltaTime, realDeltaTime);
        _time += realDeltaTime;

        if (_time > 1)
        {
            _time = 0;
            Log.Info("执行Test更新");
        }
    }
}