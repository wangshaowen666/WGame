/*--------------------------------------------------------------
 * File: ProcedureLaunch.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 16:25:31 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class ProcedureLaunch : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        Log.Info("进入启动流程");
    }

    public override void OnUpdate(float deltaTime, float realDeltaTime)
    {
        base.OnUpdate(deltaTime, realDeltaTime);
    }
}