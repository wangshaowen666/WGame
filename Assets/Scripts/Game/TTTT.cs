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
    private void Start()
    {
        Procedure.Instance.RunProcedure<ProcedureVersionCheck>();
        
       
    }
}