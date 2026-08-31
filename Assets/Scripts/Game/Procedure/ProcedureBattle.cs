/*--------------------------------------------------------------
 * File: ProcedureBattle.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/06 15:58:41 
 *--------------------------------------------------------------
 */

using System;
using cfg;
using UnityEngine;

public class ProcedureBattle : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        
        GameCamera.SetMainCamera(GameCamera.BattleCameraName);
        GameMgr.UI.PanelOn(DPnlId.BattlePanel);
        // 按预置模式分支进入战斗（临时入口在进战斗场景前设置 PendingMode；默认塔防联机）
        GameMgr.Battle.EnterBattle(GameMgr.Battle.PendingMode);
    }
}