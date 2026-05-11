/*--------------------------------------------------------------
 * File: GameEventCofig.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/30 17:51:44 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public enum GameEvent
{
    UIPanelOn = 1,
    
    // 流程相关
    ProcedureExitMain = 100,
    
    ResUpdateFinish = 200,
    
    // UI相关
    
    // 战斗相关
    EnterBattle = 2001,
}