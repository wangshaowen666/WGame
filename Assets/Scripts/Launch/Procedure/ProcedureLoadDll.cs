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
        Assembly hotEntryAss = null;
        foreach (var dll in LaunchConfig.HotfixDll)
        {
            var ass = Assembly.Load(LoadDllBytes(dll));
            if (dll.StartsWith(LaunchConfig.HotEntryDll))
                hotEntryAss = ass;
        }
#else
        // Editor下无需加载，直接查找获得HotUpdate程序集
        Assembly hotEntryAss = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == LaunchConfig.HotEntryDll);
#endif
        if (hotEntryAss == null)
        {
            Log.Error($"热更程序集{LaunchConfig.HotEntryDll} 找不到");
            return;
        }
        
        Type type = hotEntryAss.GetType(LaunchConfig.HotEntryClass);
        if (type == null)
        {
            Log.Error("未找到启动类 ", LaunchConfig.HotEntryClass);
            return;
        }
        type.GetMethod(LaunchConfig.HotEntryMethod)?.Invoke(null, null);
    }
    
    private byte[] LoadDllBytes(string key)
    {
        var handle = Addressables.LoadAssetAsync<TextAsset>(key);
        var ret = handle.WaitForCompletion();
        byte[] bytes = ret.bytes;
        Addressables.Release(handle);
        return bytes;
    }
}