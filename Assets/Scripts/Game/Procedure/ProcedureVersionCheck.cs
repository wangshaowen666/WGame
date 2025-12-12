/*--------------------------------------------------------------
 * File: ProcedureVersionCheck.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/10 11:44:33 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ProcedureVersionCheck : ProcedureBase
{
    public class MyDataObject
    {
        public string name;
        public int score;
    }
    
    public override void OnEnter()
    {
        base.OnEnter();
        AsyncRun().Forget();
    }

    private async UniTaskVoid AsyncRun()
    {
        if (true)
        {
            Procedure.Instance.RunProcedure<ProcedureResCheck>();
            return;
        }
        
        // todo 无参会报错；原生处理；
        MyDataObject data = new MyDataObject{ name = "Wsw", score = 100 };
        var serverCfg = await HttpMgr.Instance.Post(GameConfig.ServerSettingURL, data);
        int state = (int)serverCfg["ServerState"];
        switch (state)
        {
            case 1:
                Procedure.Instance.RunProcedure<ProcedureResCheck>();
                break;
            case 2:
                Log.Info("游戏维护中");
                break;
            case 3:
                Log.Info("游戏停服了");
                break;
        }
    }
}



