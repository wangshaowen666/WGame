/*--------------------------------------------------------------
 * File: TTTT.cs
 * Author: Wang ShaoWen
 * Time: 2024/01/17 18:34:03
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 * Feedback: 614270423@qq.com
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TTTT : MonoBehaviour
{
    [ContextMenu("执行")]
    public void Excute()
    {
        ClassPool pool1 = new ClassPool(typeof(A));
        ClassPool pool2 = new ClassPool(typeof(B));

        var a = pool1.Get<B>();
        var b = pool1.Get();
        //var c = pool1.Get<A>();

        var d = pool2.Get();
        
        pool2.Recycle(b);
        pool2.Recycle(d);
        //pool1.Recycle(c);
        
        pool1.Clean();
        pool2.Clean();


        var a1 = ClassPoolFactory.Get<B>();
        var b1 = ClassPoolFactory.Get(typeof(A));
        //var c1 = ClassPoolFactory.Get<A>();

        var d1 = ClassPoolFactory.Get<B>();
        
        ClassPoolFactory.Recycle(b1);
        ClassPoolFactory.Recycle(d1);
        //ClassPoolFactory.Recycle(c1);
        // ClassPoolFactory.Clean(typeof(A));
        // ClassPoolFactory.Clean(typeof(C));
        //
        ClassPoolFactory.CheckALl();
        
        
    }

    [ContextMenu("执行2")]
    public void Excute2()
    {
        List<float> f = new List<float> {2.5f, 4, 0.6f, 88, 74f, 5.1f};

        Dictionary<int, Hashtable> dict = new Dictionary<int, Hashtable>();

        for (int i = 0; i < 5; i++)
        {
            Hashtable h = new Hashtable();
            h.Add("key1", i);
            h.Add(1, "value2");
            h.Add(true, "value3");
            dict.Add(i * 2, h);
        }

        Log.Info("列表信息", f.ToJson());
        Log.Info("字典信息：", dict.ToJson());
    }
    
    
    public class A:IClass
    {
        public int m;
        public string n;

        public A(int m, string n)
        {
            this.m = m;
            this.n = n;
        }
        
        public void Init()
        {
            Log.Info(Log.LogColor.Cyan, "执行A的Init");
        }

        public void Reset()
        {
            Log.Info(Log.LogColor.Cyan, "执行A的Reset");
        }

        public void Clean()
        {
            Log.Info(Log.LogColor.Cyan, "执行A的Clean");
        }

        ~A()
        {
            Log.Info(Log.LogColor.Cyan, "A被回收");
        }

        public override string ToString()
        {
            return m + "  " + n;
        }
    }
   
    public class B : IClass
    {
        public void Init()
        {
            Log.Info(Log.LogColor.Cyan, "执行B的init");
        }

        public void Reset()
        {
            Log.Info(Log.LogColor.Cyan, "执行B的Reset");
        }

        public void Clean()
        {
            Log.Info(Log.LogColor.Cyan, "执行B的Clean");
        }

        ~B()
        {
            Log.Info(Log.LogColor.Cyan, "B被回收");
        }
    }
    
    public class C : IClass 
    {
        public void Init()
        {
            Log.Info(Log.LogColor.Cyan, "执行C的Clean");
        }

        public void Reset()
        {
            Log.Info(Log.LogColor.Cyan, "执行C的Clean");
        }

        public void Clean()
        {
            Log.Info(Log.LogColor.Cyan, "执行C的Clean");
        }
        
        ~C()
        {
            Log.Info(Log.LogColor.Cyan, "C被回收");
        }
    }
}