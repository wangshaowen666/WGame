/*--------------------------------------------------------------
 * File: TTTT.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/12 18:09:09 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

public class TTTT : MonoBehaviour
{
    [ContextMenu("执行")]
    void Test()
    {
        //GCProfiler.RecordGC(A);
        int a = 100;
        CoreMgr.Timer.StartDelay(1000, () =>
        {
            Log.Info(a);
        });
        a = 50;
    }
    
    // 测试我的自定义转换

    private void A()
    {
        for (int i = 0; i < 100; i++)
        {
            // VarInt b = i;
            // N(b);
            object c = i;
            M(c);
        }
    }

    private void M(object o)
    {
        int i = 10;
    }
    
    private void N(Variable o)
    {
        //ClassPool.Recycle(o);
    }
}

public class C
{
    //public string name = "hah";
}