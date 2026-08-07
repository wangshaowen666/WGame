/*--------------------------------------------------------------
 * File: ManagerBase.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/05/20 10:43:14 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public abstract class ManagerBase 
{
    /// <summary>
    /// 初始化
    /// </summary>
    public virtual void OnInit()
    {

    }

    /// <summary>
    /// 场景退出
    /// </summary>
    /// <param name="sceneTp">场景类型</param>
    public virtual void OnSceneExit(int sceneTp)
    {

    }

    /// <summary>
    /// 游戏重启
    /// </summary>
    public virtual void OnGameRestart()
    {

    }
}