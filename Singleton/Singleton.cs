/*--------------------------------------------------------------
 * File: Singleton.cs
 * Author: Wang ShaoWen
 * Time: 2024/03/14 15:52:25 
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

using System;
using System.Reflection;
using UnityEngine;

// 单例应该私有化构造，所以T没加new约束，通过反射实例化
public class Singleton<T> where T:Singleton<T>
{
    private static T instance;
    private static readonly object lockObject = new object();

    public static T Instance
    {
        get
        {
            lock (lockObject)
            {
                if (instance == null)
                {
                    var ctors = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

                    var ctor = Array.Find(ctors, c => c.GetParameters().Length == 0);

                    if (ctor == null)
                        throw new Exception(typeof(T).Name + "缺少无参的私有构造函数，请私有化构造");

                    instance = ctor.Invoke(null) as T;
                }
            }
            
            return instance;
        }
    }
}

