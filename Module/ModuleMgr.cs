/*--------------------------------------------------------------
 * File: GameFrameworkEntry.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/11 11:29:18 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

/// <summary>
/// 模块优先级，高的优先轮询并且晚销毁
/// 统一配置在这里，直观一点
/// </summary>
public enum ModulePriority
{
    Fsm = 100,
    
}

public static class ModuleMgr
{
    
}