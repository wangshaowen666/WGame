/*--------------------------------------------------------------
 * File: UniTaskUtil.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/05 14:08:51 
 *--------------------------------------------------------------
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public static class UniTaskUtil 
{
#region 提供给Timer的接口
    public static async UniTaskVoid DelayInvoke(int delayMs, Action<int> action, CancellationToken token, bool inMainThread = true, bool ignoreTimeScale = false)
    {
        if (action == null)
        {
            Log.Error("回调不可为空");
            return;
        }
        
        try
        {
            await UniTask.Delay(delayMs, ignoreTimeScale, cancellationToken: token);
            
            if (!inMainThread)
                await UniTask.SwitchToThreadPool();
            
            action.Invoke(1);
        }
        catch (OperationCanceledException)
        {
            Log.Info("任务被取消");
            // 脱离try的范围，回调内有错误将不会被捕获
            action.Invoke(2);
        }
        catch (Exception e)
        {
            Log.Error("任务执行出错被捕捉", e);
        }
        finally
        {
            if (!inMainThread)
                await UniTask.SwitchToMainThread();
        }
    }
    
    public static async UniTaskVoid DelayFrameInvoke(int delayFrame, Action<int> action, CancellationToken token, bool inMainThread = true)
    {
        if (action == null)
        {
            Log.Error("回调不可为空");
            return;
        }

        try
        {
            await UniTask.DelayFrame(delayFrame, cancellationToken: token);
            
            if (!inMainThread)
                await UniTask.SwitchToThreadPool();
            
            action.Invoke(1);
        }
        catch (OperationCanceledException)
        {
            Log.Info("任务被取消");
            action.Invoke(2);
        }
        catch (Exception e)
        {
            Log.Error("任务执行出错被捕捉", e);
        }
        finally
        {
            if (!inMainThread)
                await UniTask.SwitchToMainThread();
        }
    }
    
#if STATS_ON && UNITY_EDITOR
    private static readonly Unity.Profiling.ProfilerMarker s_TimerMarker = new ("WGame.Timer");
#endif
    
    public static async UniTaskVoid RepeatInvoke(int intervalMs, Action<int> action, int repeatCount = -1,
        CancellationToken token = default, bool isImmediately = false, bool inMainThread = true, bool ignoreTimeScale = false)
    {
        if (action == null)
        {
            Log.Error("回调不可为空");
            return;
        }

        if (intervalMs <= 0)
        {
            Log.Error("间隔必须大于0");
            return;
        }
        try
        {
            int count = 0;
            if (isImmediately)
            {
                if (!inMainThread)
                    await UniTask.SwitchToThreadPool();
                
                count++;
                action.Invoke(count);
            }

            while (!token.IsCancellationRequested && (repeatCount == -1 || count < repeatCount))
            {
                // 底层依赖Unity的PlayLoop判断时间到期，所以依然无法突破帧率限制
                // DelayType.Realtime不依赖Unity的帧时间，可以突破帧率
                await UniTask.Delay(intervalMs, ignoreTimeScale, cancellationToken: token);
                if (token.IsCancellationRequested) continue;
                
                if (!inMainThread)
                    await UniTask.SwitchToThreadPool();
                count++;
                action.Invoke(count);
            }
                
        }
        catch (OperationCanceledException)
        {
            Log.Info("任务被取消");
        }
        catch (Exception e)
        {
            Log.Error("任务执行出错被捕捉", e);
        }
        finally
        {
            if (!inMainThread)
                await UniTask.SwitchToMainThread();
        }
    }
    
    public static async UniTaskVoid RepeatFrameInvoke(int intervalFrame, Action<int> action, int repeatCount = -1,
        CancellationToken token = default, bool isImmediately = false, bool inMainThread = true)
    {
        if (action == null)
        {
            Log.Error("回调不可为空");
            return;
        }

        if (intervalFrame <= 0)
        {
            Log.Error("间隔必须大于0");
            return;
        }

        try
        {
            int count = 0;
            if (isImmediately)
            {
                if (!inMainThread)
                    await UniTask.SwitchToThreadPool();
                
                count++;
                action.Invoke(count);
            }

            while (!token.IsCancellationRequested && (repeatCount == -1 || count < repeatCount))
            {
                await UniTask.DelayFrame(intervalFrame, cancellationToken: token);
                if (token.IsCancellationRequested) continue;
                
                if (!inMainThread)
                    await UniTask.SwitchToThreadPool();
                count++;
                action.Invoke(count);
            }
        }
        catch (OperationCanceledException)
        {
            //Log.Info("任务被取消");
        }
        catch (Exception e)
        {
            Log.Error("任务执行出错被捕捉", e);
        }
        finally
        {
            if (!inMainThread)
                await UniTask.SwitchToMainThread();
        }
    }
#endregion
}