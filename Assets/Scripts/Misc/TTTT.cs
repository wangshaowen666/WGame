/*--------------------------------------------------------------
 * File: TTTT.cs
 * Author: Wang ShaoWen
 * Time: 2024/01/17 18:34:03
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 * Feedback: 614270423@qq.com
 *--------------------------------------------------------------
 */

using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TTTT : MonoBehaviour
{
    public int num;
    
    [ContextMenu("执行1")]
    private void Excute()
    {
        TimeProfiler.LogTime(() =>
        {
            for (int i = 0; i < num; i++)
            {
                var t = Activator.CreateInstance(typeof(A));
            }
        });
    }
    
    [ContextMenu("执行2")]
    private void Excute2()
    {
        TimeProfiler.LogTime(() =>
        {
            for (int i = 0; i < num; i++)
            {
                var t = new A();
            }
        });
    }

    private void Start()
    {
       
    }


    public class A
    {
        private string m = "jslkdfjlskjflksjlfkslflskflsldfksgfdhd";
        private int a = 999454;
    }
   
}