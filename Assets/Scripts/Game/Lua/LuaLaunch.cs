/*--------------------------------------------------------------
 * File: LuaLaunch.cs
 * Author: Wang ShaoWen
 * Time: 2025/08/04 19:23:54 
 *--------------------------------------------------------------
 */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using XLua;

public class LuaLaunch : MonoBehaviour
{
    private LuaEnv _luaEnv;
    private string[] _searchPaths;

    private void Awake()
    {
        var updater = GetComponent<HotUpdater>();
        updater.OnUpdateFinish += OnHotUpdateFinish;
    }

    private void OnHotUpdateFinish()
    {
        // Addressable的异步加载与AddLoader注册的自定义同步加载有冲突
        // 先异步加载完所有lua文件后再注册AddLoader
        ResMgr.Instance.LoadResWithLabel("lua");
        
        _luaEnv = new LuaEnv();
        _luaEnv.AddLoader(CustomLoader);
        _luaEnv.DoString("require 'Main'");
        
        ResMgr.Instance.LoadAsync<GameObject>("Cube", (t) =>
        {
            var go = Instantiate(t);
            go.transform.position = Vector3.zero;
        });
    }

    private byte[] CustomLoader(ref string filepath)
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets(filepath + ".lua"); 
        if (guids.Length > 0)
        {
            string relativePath = AssetDatabase.GUIDToAssetPath(guids[0]); 
            var t = AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath);
            return t.bytes;
            
            // string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath); 
            // if (File.Exists(absolutePath))
            // {
            //     return File.ReadAllBytes(absolutePath);
            // }
        }
        else
        {
            Debug.LogWarning("未找到文件: " + filepath);
            return null;
        }
        
        return null;
#else
        return null;
#endif
    }
}