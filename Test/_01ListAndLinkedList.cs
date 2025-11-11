/*--------------------------------------------------------------
 * File: _01ListAndNodeList.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/11 16:41:40 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 测试List和链表 遍历、插入
/// 顺序插入列表性能约为链表的30倍左右；
/// 按优先级比较插入 重复次数为100时链表性能约为列表的1.5倍，重复次数为1000时，列表性能又高于链表约1.5倍，随着遍历元素的增多链表开销增大
/// 遍历也是列表性能好，随着次数增多列表性能更优于链表，链表唯一的优势在于小数量级时的比较插入
/// </summary>
public class _01ListAndLinkedList : MonoBehaviour
{
    private int count = 100000000;
    private float time = 0;
    
    void Start()
    {
        // TimeProfiler.LogTime(T1);
        // TimeProfiler.LogTime(T2);
        //
        // TimeProfiler.RecordTimeStart("List");
        // T1();
        // TimeProfiler.RecordTimeStop("List");
        //
        // TimeProfiler.RecordTimeStart("LinkedList");
        // T2();
        // TimeProfiler.RecordTimeStop("LinkedList");
        //
        // TimeProfiler.DebugRecordTime();
        
        // TimeProfiler.LogTime(T3);
        // TimeProfiler.LogTime(T4);
        TimeProfiler.RecordTimeStart("List");
        T3();
        TimeProfiler.RecordTimeStop("List");
        
        TimeProfiler.RecordTimeStart("LinkedList");
        T4();
        TimeProfiler.RecordTimeStop("LinkedList");
        
        TimeProfiler.DebugRecordTime(true, true, true);
    }

    void Update()
    {
        if (true) return;
        
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
        for (int _ = 0; _ < 1000; _++)
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
        for (int i = 0; i < count; i++)
        {
            list.Add(i);
        }
        
        TimeProfiler.RecordTimeStart("List遍历");
        int ret = 0;
        for (int i = 0; i < list.Count; i++)
        {
            ret = list[i];
        }
        TimeProfiler.RecordTimeStop("List遍历");
    }

    private void T4()
    {
        LinkedList<int> list = new LinkedList<int>();
        for (int i = 0; i < count; i++)
        {
            list.AddLast(i);
        }
        
        TimeProfiler.RecordTimeStart("LinkedList遍历");
        var ret = list.First;
        while (ret != null)
        {
            ret = ret.Next;
        }
        TimeProfiler.RecordTimeStop("LinkedList遍历");
    }
    
    
}