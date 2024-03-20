/*--------------------------------------------------------------
 * File: IClass.cs
 * Author: Wang ShaoWen
 * Time: 2024/03/20 11:43:25 
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

/// <summary>
/// 被类对象池管理的 都需要继承此接口
/// </summary>
public interface IClass
{
    /// <summary>
    /// 初始化，每次从对象池获取时都会调用，整个生命周期只需要执行一次的可放在构造函数执行
    /// </summary>
    public void Init();
    
    /// <summary>
    /// 重置，被对象池回收时执行
    /// </summary>
    public void Reset();
    
    /// <summary>
    /// 销毁
    /// </summary>
    public void Clean();
}