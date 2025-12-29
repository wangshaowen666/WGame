/*--------------------------------------------------------------
 * File: _06UniTask.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/04 11:32:41 
 *--------------------------------------------------------------
 */

using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/*
 * 协程是一种在多帧间执行代码的机制，同步写出异步的效果，通过yield return暂停，下一帧循环到了再次执行然后暂停
 * 
 * 永远不要使用async void，异常无法被捕获，难以调试
 * 从非异步方法调用异步方法且不关注返回值，用async UniTaskVoid
 * 需要等待结果用 async UniTask或者async UniTask<T>
 *
 * 线程切换的开销通常是微秒(μs)级别的，1000微秒等于1毫秒
 * 间接开销（缓存失效）：这是经常被忽略但可能影响更大的一部分。每个CPU核心都有高速缓存（L1/L2/L3 Cache），里面存放了当前线程常用的数据和指令。
 * 当切换到新线程时，新线程需要的数据很大概率不在当前缓存里，CPU不得不花费更多时间从速度慢得多的主内存中加载数据，导致新线程在开始运行的一段时间内效率较低。
 *
 * await的机制是非阻塞的。当主线程执行到 await Test3()时，它并不会傻等着，而是会挂起当前方法，将控制权返回给Unity的游戏引擎主循环。
 * 这样，主线程就可以去处理渲染新的一帧、响应玩家输入等其他重要工作，从而保证了游戏的流畅性
 * 只有标记了async的异步方法才可以使用await，await不会阻塞主线程，只会将这个异步方法挂起
 *
 * 线程切换是“线程本地”的，只影响它所在的那个异步方法的后续执行流程。它并不会改变整个应用程序的“当前线程”全局状态 *
 *
 * UniTask.Delay依赖于Unity的PlayerLoop,它会切回主线程
 * Thread.CurrentThread.ManagedThreadId 获取线程id
 * Thread.CurrentThread.IsThreadPoolThread 判断是否在线程池，为false则在主线程
 */

public class _06UniTask : MonoBehaviour
{
    public Button btn;
    
    private const string URL = "http://localhost/MyServer";

    private bool _isFinish;
    void Start()
    {
        AsyncStart();
        //AsyncTimeTest().Forget();
    }

    async UniTaskVoid AsyncStart()
    {
        // 取消的token，当gameObject销毁时取消任务
        // var cancellationToken = this.GetCancellationTokenOnDestroy();
        //
        // // 一个task不能await两次
        // UniTask task = Test();
        // //await task;
        // Log.Info(1111);
        //
        // var str = await Test();
        // Log.Info(str);
        //
        // // 如果Test3在主线程执行，虽然3 4都不需要等待，但是3的计算耗时太久会卡住整个线程，等3计算完了 4和Update才开始继续计时
        // Test4();
        // Test3();
        //
        // Test5();
        //
        // Log.Info("并行开始");
        // UniTask task2 = Test2(cancellationToken);
        // await UniTask.WhenAll(task, task2);
        // Log.Info("并行完成");
        
        
        Log.Info(111);
        TestAwait().Forget();
        Log.Info(222);
        await TestAwait();
        Log.Info(333);
    }

    async UniTaskVoid AsyncTimeTest()
    {
        // await不是阻塞式，但是耗时方法处理会卡住整个线程  2609ms
        TimeProfiler.RecordTimeStart("执行耗时方法1");
        await Test6();
        await Test6();
        await Test6();
        await Test6();
        TimeProfiler.RecordTimeStop("执行耗时方法1");
        
        // 一起执行会快一点 2074ms
        TimeProfiler.RecordTimeStart("执行耗时方法2");
        UniTask task1 = Test6();
        UniTask task2 = Test6();
        UniTask task3 = Test6();
        UniTask task7 = Test6();
        await UniTask.WhenAll(task1, task2, task3, task7);
        TimeProfiler.RecordTimeStop("执行耗时方法2");
        
        // 切换到其他线程再回来 比一直在主线程稍稍快点 2440ms
        TimeProfiler.RecordTimeStart("执行耗时方法3");
        await Test7();
        await Test7();
        await Test7();
        await Test7();
        TimeProfiler.RecordTimeStop("执行耗时方法3");
        
        // 并行四个其他线程 耗时明显缩短 794ms
        TimeProfiler.RecordTimeStart("执行耗时方法4");
        UniTask task4 = Test7();
        UniTask task5 = Test7();
        UniTask task6 = Test7();
        UniTask task8 = Test7();
        await UniTask.WhenAll(task4, task5, task6, task8);
        TimeProfiler.RecordTimeStop("执行耗时方法4");
        
        TimeProfiler.LogDurationTime();
    }

    async UniTask TestAwait()
    {
        await UniTask.Delay(5000);
    }
    
    async UniTask<string> Test()
    {
        string fileNm = "耗时统计.txt";
        string filePath = Path.Combine(URL, fileNm);
        string saveDirectory = Path.Combine(Application.persistentDataPath, "Downloads");
        string fullPath = Path.Combine(saveDirectory, fileNm);
        
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        using (UnityWebRequest webRequest = UnityWebRequest.Get(filePath))
        {
            await webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Log.Info("下载出错" + webRequest.error);
            }
            else
            {
                string content = webRequest.downloadHandler.text;
                Log.Info("文件内容:", content);
                
                byte[] bytes = webRequest.downloadHandler.data;
                await File.WriteAllBytesAsync(fullPath, bytes);
                Log.Info("文件已保存至：", fullPath);
            }
        }

        return "aa";
    }

    private float _timer;
    private float _temp = 0;
    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer - _temp > 1)
        {
            _temp = _timer;
            Log.Info("Update 1s打印");
        }
        
        
        if (_timer > 80f && !_isFinish)
        {
            _isFinish = true;
        }
    }

    async UniTask Test2(CancellationToken token)
    {
        // 等待10毫秒,不受时间缩放影响
        await UniTask.Delay(10, ignoreTimeScale: true, cancellationToken: token);
        
        // 等待10帧
        await UniTask.DelayFrame(10, cancellationToken: token);
        
        // 等待5s
        await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: token);
        
        // 等一帧
        await UniTask.NextFrame(token);
        await UniTask.Yield(token);
        
        // 可以等待任何PlayerLoop的生命周期
        await UniTask.Yield(PlayerLoopTiming.Update, token);
        await UniTask.Yield(PlayerLoopTiming.LastUpdate, token);

        // 每帧检测状态，返回true结束等待
        await UniTask.WaitUntil(() =>
        {
            //Log.Info("检查_isFinish状态");
            return _isFinish;
        }, cancellationToken: token);

        // 每帧检测值变化，值与初始不相等则结束等待
        await UniTask.WaitUntilValueChanged(this, x =>
        {
            //Log.Info("检测_temp变化", x._temp, _temp);
            return x._temp;
        }, cancellationToken: token);
    }

    async UniTaskVoid Test3()
    {
        Log.Info("主线程执行中...");

        // 切换到后台线程
        await UniTask.SwitchToThreadPool();
        Log.Info("切换到后台线程");
        Calculate();
        Log.Info("执行完成");

        // 如果不切回主线程，则后续程序都将在后台线程执行，遇到Unity自身的大多数API都会出问题
        await UniTask.SwitchToMainThread();
        Log.Info("回到主线程");
    }

    async UniTaskVoid Test4()
    {
        Log.Info("执行Test4");
        await UniTask.Delay(TimeSpan.FromSeconds(5));
        Log.Info("完成Test4");
    }

    async UniTaskVoid Test5()
    {
        while (btn != null)
        {
            await btn.OnClickAsync();
            Log.Info("按钮被点击");
        }
    }

    
    async UniTask Test6()
    {
        await UniTask.Yield();
        Calculate();
    }
    
    async UniTask Test7()
    {
        await UniTask.SwitchToThreadPool();
        Calculate();
        await UniTask.SwitchToMainThread();
    }
    
    // 运算耗时测试
    float Calculate()
    {
        float ret = 0;
        for (int j = 0; j < 10000; j++)
        {
            for (int i = 1; i < 10000; i++)
            {
                ret += ((i + (i + 1)) * 3 - i) / 5f;
            }

            ret = 0;
        }
        
        return ret;
    }
}