/*--------------------------------------------------------------
 * File: BattleBase.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/26 14:40:45 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public enum BattleMode : byte
{
    Survival,   // 生存模式
}

public abstract class BattleBase 
{
    public virtual void Init()
    {
        
    }
}