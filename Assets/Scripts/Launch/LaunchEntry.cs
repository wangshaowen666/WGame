/*--------------------------------------------------------------
 * File: GameEntry.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/12 16:41:23 
 *--------------------------------------------------------------
 */

using UnityEngine;

public class LaunchEntry : MonoBehaviour
{
    void Start()
    {
        InitProcedure();
        DontDestroyOnLoad(gameObject);
    }
    
    void Update()
    {
        CoreMgr.Update.MyUpdate(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void InitProcedure()
    {
        Procedure.AddProcedure(new ProcedureLaunch());
        Procedure.AddProcedure(new ProcedureVersionCheck());
        Procedure.AddProcedure(new ProcedureResCheck_AA());
        Procedure.AddProcedure(new ProcedureLoadDll());
        
        Procedure.ChangeProcedure<ProcedureLaunch>();
    }
}