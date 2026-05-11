/*--------------------------------------------------------------
 * File: PlayerShip.cs
 * Author: Wang ShaoWen
 * Time: 2026/05/11 11:41:30 
 *--------------------------------------------------------------
 */

using System;
using cfg;
using UnityEngine;

public class PlayerShip : EntityBase
{
    private DAircraft _cfg;
    public override void OnInit(int id)
    {
        base.OnInit(id);
        _cfg = DataTableMgr.Instance.TbAircraft[id];
        
        CreateEngine();
    }

    private void CreateEngine()
    {
        var root = transform.Find("Thruster Point");
        EntityMgr.Instance.ShowEntity(_cfg.ThrusterId, root, null);
    }
}