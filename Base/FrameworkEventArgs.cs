/*--------------------------------------------------------------
 * File: FrameworkEventArgs.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/13 16:05:38 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public abstract class FrameworkEventArgs : EventArgs, IResetable
{
    public abstract void Reset();
}