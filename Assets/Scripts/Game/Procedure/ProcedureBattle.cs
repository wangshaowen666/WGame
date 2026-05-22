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
        
        FrameworkMgr.Screen.SetMainCamera(FrameworkConfig.BattleCamera);
        GameMgr.UI.PanelOn(DPnlId.BattlePanel);
        GameMgr.Battle.EnterBattle();
    }
}