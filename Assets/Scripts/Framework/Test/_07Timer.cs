/*--------------------------------------------------------------
 * File: _07Timer.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/05 15:46:38 
 *--------------------------------------------------------------
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class _07Timer : MonoBehaviour
{
    private CancellationTokenSource _cts;

    private _05Test _t;
    
    private void Start()
    {
        WaitKeyDown().Forget();

        //_cts = Timer.StartDelay(5000, TimerDelayTest, false);
        _cts = Timer.StartRepeat(500, TimerRepeatTest, 20, false, false);
    }

    async UniTaskVoid WaitKeyDown()
    {
        while (true)
        {
            await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
            _cts?.Cancel();
        }
    }

    private void TimerDelayTest(int state)
    {
        Log.Info("延迟执行", state);
        //Log.Info(_t.GetType());
        transform.position += Vector3.one;
    }

    private void TimerRepeatTest(int count)
    {
        transform.position += Vector3.one;
        Log.Info("重复执行", count);
    }
}