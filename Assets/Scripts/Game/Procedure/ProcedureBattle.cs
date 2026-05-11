/*--------------------------------------------------------------
 * File: ProcedureBattle.cs
 * Author: Wang ShaoWen
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
        
        ScreenCtr.Instance.SetMainCamera(GameConfig.BattleCamera);
        UIMgr.Instance.PanelOn(DPnlId.BattlePanel);
        BattleMgr.Instance.EnterBattle();
    }
}