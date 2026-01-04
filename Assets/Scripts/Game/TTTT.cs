/*--------------------------------------------------------------
 * File: TTTT.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 18:09:09 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TTTT : MonoBehaviour
{
    private enum MyEnum
    {
        OnLevelUp = 1,
    }
    
    private string str;
    private List<string> hashNmd = new List<string>();
    
    private void Start()
    {
        Procedure.Instance.RunProcedure<ProcedureVersionCheck>();
    }

    [ContextMenu("执行")]
    void Execute()
    {
        for (int i = 0; i < 10; i++)
        {
            str = i.ToString();
            hashNmd.Add(str);
        }
        
        Log.Info("结果", ParseUtil.ToJson(hashNmd));
    }
}