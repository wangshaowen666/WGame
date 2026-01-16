/*--------------------------------------------------------------
 * File: IClass.cs
 * Author: Wang ShaoWen
 * Time: 2024/03/20 11:43:25 
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

/// <summary>
/// 被类对象池管理的 都需要继承此接口
/// </summary>
public interface IResetable
{
    /// <summary>
    /// 重置，被对象池回收时执行
    /// </summary>
    public void Reset();
}