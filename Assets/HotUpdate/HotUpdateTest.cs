/*--------------------------------------------------------------
 * File: HotUpdateTest.cs
 * Author: Wang ShaoWen
 * Time: 2024/01/17 18:49:21
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 * Feedback: 614270423@qq.com
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class HotUpdateTest 
{
    public static void Run()
    {
        Debug.Log("Hello World");

        GameObject go = new GameObject("Test1");
        go.AddComponent<Print>();

        TT t = new TT {a = 1, b = "ee"};
        List<TT> tLst = new List<TT>{t};

        foreach (var _t in tLst)
        {
            Debug.Log($"[TT] t.a:{_t.a}   t.b:{_t.b}");
        }

        List<float> l = new List<float> {10, 12.2f, 13};
        foreach (var i in l)
        {
            Debug.Log("[List<float>]:" + i);
        }
    }
    
    public class TT
    {
        public int a;
        public string b;
    }
}