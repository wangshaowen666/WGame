/*--------------------------------------------------------------
 * File: ProcedurePreload.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/13 13:36:48 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class ProcedurePreload : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        Log.Info("进入预加载流程");
    }
}