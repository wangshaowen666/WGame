/*--------------------------------------------------------------
 * File: BattleMgr.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/26 14:53:41 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class BattleMgr : Singleton<BattleMgr>
{
    private BattleBase _battle;

    private BattleMgr()
    {
    }

    public void Init()
    {
        InitEvent();
    }

    private void InitEvent()
    {
        EventMgr.Instance.Register(GameEvent.EnterBattle, OnEnterBattle);
    }

    private void OnEnterBattle()
    {
        _battle = new BattleSurvival();
        _battle.Init();
    }
}