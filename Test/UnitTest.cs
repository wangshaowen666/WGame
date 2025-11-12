/*--------------------------------------------------------------
 * File: UnitTest.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/11 14:37:57 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitTest : MonoBehaviour
{
    private TestA a;
    private TestB b;
    void Start()
    {
        var a = CoroutineRunner.Instance.Run(T());
        CoroutineRunner.Instance.Stop(a);
    }

    private IEnumerator T()
    {
        yield return new WaitForSeconds(1);
    }

    void Update()
    {
        UpdateMgr.Update(Time.deltaTime, Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.A))
        {
            UpdateMgr.UnRegisterUpdate(a);
        }
        
        if (Input.GetKeyDown(KeyCode.B))
        {
            UpdateMgr.RegisterUpdate(a);
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            a = new TestA();
            b = new TestB();
            
            UpdateMgr.RegisterUpdate(a);
            UpdateMgr.RegisterUpdate(b);
        }
    }
}

public class TestA : IUpdateable
{
    private float time;

    public void Update(float logicTime, float realTime)
    {
        time += logicTime;
        if (time > 1)
        {
            time = 0;
            Log.Info("TestA执行Update");
        }
    }
}

public class TestB : IUpdateable
{
    private float time;

    public void Update(float logicTime, float realTime)
    {
        time += logicTime;
        if (time > 1)
        {
            time = 0;
            Log.Info("TestB执行Update");
        }
    }
}