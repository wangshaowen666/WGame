/*--------------------------------------------------------------
 * File: BattleSurvival.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/26 14:49:35 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class BattleSurvival : BattleBase
{
    private Aircraft _playerAircraft;
    
    public override void Init()
    {
        base.Init();
        CreatePlayerAircraft(10000);
    }

    private void CreatePlayerAircraft(int eId)
    {
        _playerAircraft = ClassPool.Get<Aircraft>();
        _playerAircraft.Init(eId);
    }
}