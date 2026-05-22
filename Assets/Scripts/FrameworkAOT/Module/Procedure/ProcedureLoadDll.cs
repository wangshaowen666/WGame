/*--------------------------------------------------------------
 * File: ProcedureLoadDll.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/05 14:10:17 
 *--------------------------------------------------------------
 */

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ProcedureLoadDll : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
#if !UNITY_EDITOR
        // 这里单独实现的脚本加载没走资源管理系统，因为资源管理本身属于热更脚本
        Assembly.Load(LoadDllBytes("Framework.JIT.dll"));
        Assembly hotUpdateAss = Assembly.Load(LoadDllBytes("Game.dll"));
#else
        // Editor下无需加载，直接查找获得HotUpdate程序集
        Assembly hotUpdateAss = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Game");
#endif
        Type type = hotUpdateAss.GetType("GameLaunch");
        type.GetMethod("StartGame").Invoke(null, null);
    }
    
    private byte[] LoadDllBytes(string key)
    {
        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(key);
        if (handle.IsDone)
        {
            return handle.Result.bytes;
        }

        var ret = handle.WaitForCompletion();
        return ret.bytes;
    }
}