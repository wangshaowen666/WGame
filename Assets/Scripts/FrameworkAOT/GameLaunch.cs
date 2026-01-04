/*--------------------------------------------------------------
 * File: TTTT.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 16:41:23 
 *--------------------------------------------------------------
 */

using UnityEngine;

public class GameLaunch : MonoBehaviour
{
    void Start()
    {
        //DontDestroyOnLoad(gameObject);
        Procedure.Instance.RunProcedure<ProcedureLaunch>();
    }
    
    void Update()
    {
        //UpdateMgr.Update(Time.deltaTime, Time.unscaledDeltaTime);
    }
}