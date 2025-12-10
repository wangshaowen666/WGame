/*--------------------------------------------------------------
 * File: ResMgr.cs
 * Author: Wang ShaoWen
 * Time: 2025/09/01 10:55:17 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class ResMgr : Singleton<ResMgr>
{
    private readonly IResLoader _resLoader;

    private ResMgr()
    {
        _resLoader = new AddressableLoader();
    }

    public T LoadSync<T>(string key) where T : class
    {
        return _resLoader.LoadSync<T>(key);
    }

    public void LoadAsync<T>(string key, Action<T> callback) where T : class
    {
        _resLoader.LoadAsync(key, callback);
    }

    public void LoadResWithLabel(string label)
    {
        _resLoader.LoadAsyncWithLabel(label);
    }
}