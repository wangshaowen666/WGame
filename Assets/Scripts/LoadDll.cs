/*--------------------------------------------------------------
 * File: LoadDll.cs
 * Author: Wang ShaoWen
 * Time: 2024/01/17 18:53:05
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 * Feedback: 614270423@qq.com
 *--------------------------------------------------------------
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class LoadDll : MonoBehaviour
{
    private void Start()
    {
#if UNITY_EDITOR
        Assembly hotUpdateAss =
            System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
#else
        Assembly hotUpdateAss = Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/HotUpdate.dll.bytes"));
#endif

        Type type = hotUpdateAss.GetType("HotUpdateTest");
        type.GetMethod("Run").Invoke(null, null);
    }
}