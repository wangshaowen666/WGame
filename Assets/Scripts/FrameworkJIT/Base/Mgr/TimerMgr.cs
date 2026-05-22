/*--------------------------------------------------------------
 * File: TimerMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/05/22 16:09:58 
 *--------------------------------------------------------------
 */

using System;
using System.Threading;
    
public class TimerMgr : ManagerBase
{
    /// <summary>
    /// 等待指定毫秒后执行
    /// </summary>
    /// <param name="delayMs">等待毫秒</param>
    /// <param name="action">要执行的方法</param>
    /// <param name="inMainThread">是否在主线程执行</param>
    /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
    public CancellationTokenSource StartDelay(int delayMs, Action<int> action, bool inMainThread = true, bool ignoreTimeScale = false)
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
    public CancellationTokenSource StartSecondDelay(float delaySd, Action<int> action, bool inMainThread = true, bool ignoreTimeScale = false)
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
    public CancellationTokenSource StartFrameDelay(int delayFrame, Action<int> action, bool inMainThread = true)
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
    public CancellationTokenSource StartRepeat(int intervalMs, Action<int> action, int repeatCount = -1,
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
    public CancellationTokenSource StartFrameRepeat(int intervalFrame, Action<int> action, int repeatCount = -1,
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
    public void Stop(CancellationTokenSource cts)
    {
        cts.Cancel();
    }
}