/*--------------------------------------------------------------
 * File: ReadDataSuccessEventArgs.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/13 16:07:55 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public sealed class ReadDataSuccessEventArgs : FrameworkEventArgs
{
    /// <summary>
    /// 获取内容资源名称。
    /// </summary>
    public string DataAssetName
    {
        get;
        private set;
    }

    /// <summary>
    /// 获取加载持续时间。
    /// </summary>
    public float Duration
    {
        get;
        private set;
    }

    /// <summary>
    /// 获取用户自定义数据。
    /// </summary>
    public object UserData
    {
        get;
        private set;
    }
    
    public static ReadDataSuccessEventArgs Create(string dataAssetName, float duration, object userData)
    {
        ReadDataSuccessEventArgs args = ClassFactory.Get<ReadDataSuccessEventArgs>();
        args.DataAssetName = dataAssetName;
        args.Duration = duration;
        args.UserData = userData;
        return args;
    }
    
    public override void Reset()
    {
        DataAssetName = null;
        Duration = 0f;
        UserData = null;
    }
}