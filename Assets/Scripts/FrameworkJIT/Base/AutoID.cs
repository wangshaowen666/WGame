/*--------------------------------------------------------------
 * File: AutoID.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/27 17:27:47 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public static class AutoID
{
    private static uint _id = 0;

    public static uint GetID()
    {
        return ++_id;
    }
}