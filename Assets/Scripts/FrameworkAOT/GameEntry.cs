/*--------------------------------------------------------------
 * File: TTTT.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 16:41:23 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using UnityEngine;

public class GameEntry : MonoBehaviour
{
    void Start()
    {
        ProcedureMgr.Instance.RunProcedure<ProcedureLaunch>();
    }
    
    void Update()
    {
        //UpdateMgr.Update(Time.deltaTime, Time.unscaledDeltaTime);
    }
}