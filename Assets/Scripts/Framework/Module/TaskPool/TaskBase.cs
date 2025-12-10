/*--------------------------------------------------------------
 * File: TaskBase.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/27 17:16:48 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class TaskBase : IResetable
{
    // 用户自定义参数，多个用集合的形式；会有装箱的情况；
    private object _userData;

    public uint TaskID { get; private set; }
    public int Priority { get; private set; }
    public bool IsDown { get; private set; }

    public void Init(int priority = 0, object userData = null)
    {
        TaskID = AutoID.GetID();
        Priority = priority;
        _userData = userData;
    }
    
    public void Reset()
    {
        TaskID = 0;
        Priority = 0;
        IsDown = false;
        
        _userData = null;
    }
}