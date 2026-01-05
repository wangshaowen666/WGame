/*--------------------------------------------------------------
 * File: ProcedureLoadDll.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/05 14:10:17 
 *--------------------------------------------------------------
 */

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class ProcedureLoadDll : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
#if !UNITY_EDITOR
        Assembly hotUpdateAss = Assembly.Load(ResMgr.Instance.LoadSync<TextAsset>("Game.dll").bytes);
#else
        // Editor下无需加载，直接查找获得HotUpdate程序集
        Assembly hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Game");
#endif
        Type type = hotUpdateAss.GetType("GameLaunch");
        type.GetMethod("StartGame").Invoke(null, null);
    }
}