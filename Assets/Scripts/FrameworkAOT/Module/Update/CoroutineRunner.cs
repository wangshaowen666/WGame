/*--------------------------------------------------------------
 * File: CoroutineRunner.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/09/01 17:40:04 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using UnityEngine;

public class CoroutineRunner : MonoSingleton<CoroutineRunner>
{
    public Coroutine Run(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }

    public void Stop(Coroutine routine)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }
    }
}