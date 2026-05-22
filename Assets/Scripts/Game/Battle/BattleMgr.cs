/*--------------------------------------------------------------
 * File: BattleMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/26 14:53:41 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class BattleMgr : ManagerBase
{
    public BattleBase CurrentBattle => _battle;
    private BattleBase _battle;

    public void EnterBattle()
    {
        _battle = new BattleSurvival();
        _battle.Init();
    }

    public override void OnSceneExit(int sceneTp)
    {
        base.OnSceneExit(sceneTp);
        if (sceneTp == 2)
        {
            _battle = null;
        }
    }

    public override void OnGameRestart()
    {
        base.OnGameRestart();
        _battle = null;
    }
}