/*--------------------------------------------------------------
 * File: BattleBase.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
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