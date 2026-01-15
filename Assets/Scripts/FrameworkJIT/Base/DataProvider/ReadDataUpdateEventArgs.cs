/*--------------------------------------------------------------
 * File: ReadDataUpdateEventArgs.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/13 16:15:01 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class ReadDataUpdateEventArgs : FrameworkEventArgs
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
    /// 获取读取数据进度。
    /// </summary>
    public float Progress
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
    
    public static ReadDataUpdateEventArgs Create(string dataAssetName, float progress, object userData)
    {
        ReadDataUpdateEventArgs args = ClassFactory.Instance.Get<ReadDataUpdateEventArgs>();
        args.DataAssetName = dataAssetName;
        args.Progress = progress;
        args.UserData = userData;
        return args;
    }
    
    public override void Reset()
    {
        DataAssetName = null;
        Progress = 0f;
        UserData = null;
    }
}