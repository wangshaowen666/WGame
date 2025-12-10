/*--------------------------------------------------------------
 * File: TTTT.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 18:09:09 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TTTT : MonoBehaviour
{

    private MyEvent<int> OnChange;

    private Action<float, int, string> On;
    
    private void Awake()
    {
        
        
    }

    private void T(object obj)
    {
        var a = AutoID.GetID();
        Log.Info(Log.LogColor.Cyan, a);
        a = AutoID.GetID();
        Log.Info(Log.LogColor.Cyan, a);
        a = AutoID.GetID();
        Log.Info(Log.LogColor.Cyan, a);
        a = AutoID.GetID();
        Log.Info(Log.LogColor.Cyan, a);
    }
}

public delegate void MyEvent<T>(T t1);
public delegate void MyEvent<T1, T2>(T1 t1, T2 t2);
