/*--------------------------------------------------------------
 * File: GameEntry.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/12 16:41:23 
 *--------------------------------------------------------------
 */

using UnityEngine;

public class GameEntry : MonoBehaviour
{
    void Start()
    {
        ProcedureMgr.RunProcedure<ProcedureLaunch>();
        DontDestroyOnLoad(gameObject);
    }
    
    void Update()
    {
        UpdateMgr.Update(Time.deltaTime, Time.unscaledDeltaTime);
    }
}