/*--------------------------------------------------------------
 * File: ProcedurePreload.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/13 13:36:48 
 *--------------------------------------------------------------
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ProcedurePreload : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        Log.Info("进入预加载流程");
        
        AsyncRun().Forget();
    }
    
    private async UniTaskVoid AsyncRun()
    {
        var panel = _fsm.GetObj<LoginPanel>("loginPanel");
        panel.SetTip("编译着色器中...", 0.9f);
        // todo 预加载配置表、图集、字体等

        await UniTask.Delay(300);
        
        //_fsm.SetObj("sceneNm", "Main");
        //Procedure.Instance.RunProcedure<ProcedureChangeScene>();
        
#if !UNITY_EDITOR
        Assembly hotUpdateAss = Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/HotUpdate.dll.bytes"));
#else
        // Editor下无需加载，直接查找获得HotUpdate程序集
        Assembly hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Game");
#endif
        Type type = hotUpdateAss.GetType("GameStart");
        type.GetMethod("StartGame").Invoke(null, null);
    }
}