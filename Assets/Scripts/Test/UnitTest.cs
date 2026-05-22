/*--------------------------------------------------------------
 * File: UnitTest.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/11 14:37:57 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiniJSON;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class UnitTest : MonoBehaviour
{
    private void Start()
    {
       
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Log.Info("空格按下");
        }
    }
}