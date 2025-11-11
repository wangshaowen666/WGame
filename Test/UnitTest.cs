/*--------------------------------------------------------------
 * File: UnitTest.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/11 14:37:57 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitTest : MonoBehaviour
{
    private int count = 100000;
    private float time = 0;
    
    void Start()
    {
    }

    void Update()
    {
        time += Time.deltaTime;
        TimeProfiler.RecordTimeStart("List");
        T1();
        TimeProfiler.RecordTimeStop("List");
        
        TimeProfiler.RecordTimeStart("LinkedList");
        T2();
        TimeProfiler.RecordTimeStop("LinkedList");

        if (time > 5)
        {
            time = 0;
            TimeProfiler.DebugRecordTime(true, true, true);
        }
    }

    private void T1()
    {
        List<int> a = new List<int> { 5, 1, 4, 12, 65, 2, 8, 45, 96, 7, 85, 22, 1 };
        List<int> list = new List<int>();
        for (int _ = 0; _ < 100; _++)
        {
            for (int i = 0; i < a.Count; i++)
            {
                if (list.Count > 1)
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j] > a[i])
                        {
                            list.Insert(j, a[i]);
                            break;
                        }
                    }
                
                    list.Add(a[i]);
                }
                else
                {
                    list.Add(a[i]);
                }
            }
        }
    }
    
    private void T2()
    {
        List<int> a = new List<int> { 5, 1, 4, 12, 65, 2, 8, 45, 96, 7, 85, 22, 1 };
        LinkedList<int> list = new LinkedList<int>();
        
        for (int _ = 0; _ < 100; _++)
        {
            for (int i = 0; i < a.Count; i++)
            {
                var m = list.First;
                while (m != null)
                {
                    if (m.Value > a[i])
                    {
                        break;
                    }

                    m = m.Next;
                }

                if (m != null)
                {
                    list.AddBefore(m, a[i]);
                }
                else
                {
                    list.AddLast(a[i]);
                }
            }
        }
    }

    private void T3()
    {
        List<int> list = new List<int>();
    }
}