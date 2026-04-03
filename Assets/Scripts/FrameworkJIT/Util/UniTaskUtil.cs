/*--------------------------------------------------------------
 * File: UniTaskUtil.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/05 14:08:51 
 *--------------------------------------------------------------
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class Timer
{
    /// <summary>
    /// 等待指定毫秒后执行
    /// </summary>
    /// <param name="delayMs">等待毫秒</param>
    /// <param name="action">要执行的方法</param>
    /// <param name="inMainThread">是否在主线程执行</param>
    /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
    public static CancellationTokenSource StartDelay(int delayMs, Action<int> action, bool inMainThread = true, bool ignoreTimeScale = false)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        UniTaskUtil.DelayInvoke(delayMs, action, cts.Token, inMainThread, ignoreTimeScale).Forget();
        return cts;
    }
    
    /// <summary>
    /// 等待指定秒后执行，推荐使用StartDelay毫秒方法，少一点运算量
    /// </summary>
    /// <param name="delaySd">等待秒</param>
    /// <param name="action">要执行的方法</param>
    /// <param name="inMainThread">是否在主线程执行</param>
    /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
    public static CancellationTokenSource StartSecondDelay(float delaySd, Action<int> action, bool inMainThread = true, bool ignoreTimeScale = false)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        UniTaskUtil.DelayInvoke((int)(delaySd * 1000f), action, cts.Token, inMainThread, ignoreTimeScale).Forget();
        return cts;
    }
    
    /// <summary>
    /// 等待指定帧后执行
    /// </summary>
    /// <param name="delayFrame">等待帧数</param>
    /// <param name="action">要执行的方法</param>
    /// <param name="inMainThread">是否在主线程执行</param>
    public static CancellationTokenSource StartFrameDelay(int delayFrame, Action<int> action, bool inMainThread = true)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        UniTaskUtil.DelayFrameInvoke(delayFrame, action, cts.Token, inMainThread).Forget();
        return cts;
    }

    /// <summary>
    /// 间隔毫秒后重复执行
    /// </summary>
    /// <param name="intervalMs">间隔毫秒</param>
    /// <param name="action">要执行的方法</param>
    /// <param name="repeatCount">重复次数</param>
    /// <param name="isImmediately">是否立刻执行一次</param>
    /// <param name="inMainThread">是否在主线程执行</param>
    /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
    public static CancellationTokenSource StartRepeat(int intervalMs, Action<int> action, int repeatCount = -1,
        bool isImmediately = false, bool inMainThread = true, bool ignoreTimeScale = false)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        UniTaskUtil.RepeatInvoke(intervalMs, action, repeatCount, cts.Token, isImmediately, inMainThread, ignoreTimeScale).Forget();
        return cts;
    }
    
    /// <summary>
    /// 间隔帧后重复执行
    /// </summary>
    /// <param name="intervalFrame">间隔帧</param>
    /// <param name="action">要执行的方法</param>
    /// <param name="repeatCount">重复次数</param>
    /// <param name="isImmediately">是否立刻执行一次</param>
    /// <param name="inMainThread">是否在主线程执行</param>
    public static CancellationTokenSource StartFrameRepeat(int intervalFrame, Action<int> action, int repeatCount = -1,
        bool isImmediately = false, bool inMainThread = true)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        UniTaskUtil.RepeatFrameInvoke(intervalFrame, action, repeatCount, cts.Token, isImmediately, inMainThread).Forget();
        return cts;
    }

    /// <summary>
    /// 结束计时
    /// </summary>
    /// <param name="cts">取消令牌</param>
    public static void Stop(CancellationTokenSource cts)
    {
        cts.Cancel();
    }
}


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
            Log.Info("执行完成");
        }
    }
#endregion
}