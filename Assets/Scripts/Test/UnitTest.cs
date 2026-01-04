/*--------------------------------------------------------------
 * File: UnitTest.cs
 * Author: Wang ShaoWen
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
using UnityEngine.Profiling;
using UnityEngine.UI;

public class UnitTest : MonoBehaviour
{
    private void Start()
    {
        Timer.StartDelay(2000, i =>
        {
            EventCenter.Instance.Send(GameEvent.ResUpdateFinish, 3);
        });

        Action<int> ac = (i) =>
        {
            Log.Info(i);
        };
        
        EventCenter.Instance.Register(GameEvent.ResUpdateFinish, ac);
        EventCenter.Instance.Register(GameEvent.ResUpdateFinish, ac);
        EventCenter.Instance.UnRegister(GameEvent.ResUpdateFinish, ac);
        
        EventCenter.Instance.Register<int>(GameEvent.ResUpdateFinish, Test1);
        EventCenter.Instance.Register<int>(GameEvent.ResUpdateFinish, Test1);
        EventCenter.Instance.UnRegister<int>(GameEvent.ResUpdateFinish, Test1);

        A a = new A();
        EventCenter.Instance.Register<int>(GameEvent.ResUpdateFinish, a.T);
        a = new A();
        EventCenter.Instance.UnRegister<int>(GameEvent.ResUpdateFinish, a.T);
    }

    private void Test1(int i)
    {
        Log.Info(i + 1);
    }
}

public class A
{
    public void T(int i)
    {
        Log.Info("执行A中的方法T:", i);
    }
}