/*--------------------------------------------------------------
 * File: _08IfAndSwitch.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/08 13:28:29 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

/*
 * switch有一张编译器生成的跳转表，定位操作时间复杂度为O(1),而if 是从上到下逐个条件判断，时间平均复杂度为O(n)
 * 以值类型比较作为判断条件，二者耗时差不多，switch小优
 * 以引用类型比较作为判断条件，switch好于if，有时if耗时是switch的几倍
 */
public class _08IfAndSwitch : MonoBehaviour
{
    private int _count = 10000;
    
    void Start()
    {
        CoreMgr.Timer.StartDelay(5000, () =>
        {
            TimeProfiler.LogDurationTime();
        });
    }

    void Update()
    {
        TimeProfiler.RecordTimeStart("If耗时");
        for (int i = 0; i < _count; i++)
        {
            //IfTest(70);
            IfTest("9");
        }
        TimeProfiler.RecordTimeStop("If耗时");
        
        
        TimeProfiler.RecordTimeStart("Switch耗时");
        for (int i = 0; i < _count; i++)
        {
            //SwitchTest(70);
            SwitchTest("9");
        }
        TimeProfiler.RecordTimeStop("Switch耗时");
    }

    private void IfTest(int i)
    {
        if (i == 0)
        {
            
        }
        else if (i == 1)
        {
            
        }
        else if (i == 2)
        {
            
        }
        else if (i == 3)
        {
            
        }
        else if (i == 4)
        {
            
        }
        else if (i == 5)
        {
            
        }
        else if (i == 6)
        {
            
        }
        else
        {
            
        }
    }

    private void SwitchTest(int i)
    {
        switch (i)
        {
            case 0:
                break;
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
            case 4:
                break;
            case 5:
                break;
            case 6:
                break;
            default:
                break;
        }
    }
    
    private void IfTest(string i)
    {
        if (i == "0")
        {
            
        }
        else if (i == "1")
        {
            
        }
        else if (i == "2")
        {
            
        }
        else if (i == "3")
        {
            
        }
        else if (i == "4")
        {
            
        }
        else if (i == "5")
        {
            
        }
        else if (i == "6")
        {
            
        }
        else
        {
            
        }
    }

    private void SwitchTest(string i)
    {
        switch (i)
        {
            case "0":
                break;
            case "1":
                break;
            case "2":
                break;
            case "3":
                break;
            case "4":
                break;
            case "5":
                break;
            case "6":
                break;
            default:
                break;
        }
    }
}

