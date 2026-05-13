/*--------------------------------------------------------------
 * File: GameEntry.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/12 16:41:23 
 *--------------------------------------------------------------
 */

using UnityEngine;

public class GameEntry : MonoBehaviour
{
    void Start()
    {
        ProcedureMgr.Instance.RunProcedure<ProcedureLaunch>();
        DontDestroyOnLoad(gameObject);
    }
    
    void Update()
    {
        UpdateMgr.Update(Time.deltaTime, Time.unscaledDeltaTime);
    }
}