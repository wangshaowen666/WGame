/*--------------------------------------------------------------
 * File: ProcudureResCheck.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/11 14:09:47 
 *--------------------------------------------------------------
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public class ProcedureResCheck_AA : ProcedureBase
{
    private const int MB = 1024 * 1024;
    
    private AddressableHelper _helper;
    private CancellationTokenSource _tokenSource;
    private LoginPanel _loginPanel;
    private int _failTp;

    public override void OnInit(Fsm fsm)
    {
        base.OnInit(fsm);
        _helper = new AddressableHelper();
        _helper.OnLoadFail += OnHelperLoadFail;
        _helper.OnDownloadProgress += OnHelperDownloadProgress;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        
        _loginPanel = _fsm.GetData<LoginPanel>("loginPanel");
        _tokenSource = new CancellationTokenSource();
        AsyncRun().Forget();
    }
    
    public override void OnExit()
    {
        _tokenSource?.Cancel();
        _tokenSource?.Dispose();
        _tokenSource = null;
    }

    private async UniTaskVoid AsyncRun()
    {
        // 协作式取消，异步编程中官方推荐做法。
        try
        {
            await _helper.InitAsync();
            
            _loginPanel.SetTip("更新资源列表...", 0.3f);
            await _helper.UpdateCatalog(_tokenSource.Token);
            
            _loginPanel.SetTip("资源检查中...", 0.4f);
            await _helper.CheckRes(_tokenSource.Token);
            
            await _helper.Download(_tokenSource.Token);
            
            ChangeTo<ProcedureLoadDll>();
        }
        catch (OperationCanceledException e)
        {
            Log.Info("任务出错，被取消");
            if (_failTp == 1)
            {
                ChangeTo<ProcedureLoadDll>();
            }
        }
        catch (Exception e)
        {
            Log.Error("更新执行出错:", e.Message, e.InnerException, e.StackTrace);
        }
    }
    
    private void OnHelperLoadFail(int result)
    {
        Log.Info("执行加载出错回调");
        _tokenSource.Cancel();
        _failTp = result;
        
        //Cancel是同步的,AsyncRun 的 await 返回是异步的。 RunProcedure 同步触发 ChangeState ，此时 AsyncRun 可能还没进入 catch。
        //如果后续在 catch 中加逻辑就会有竞态。
        
        // switch (result)
        // {
        //     case 1:
        //         // 尝试跳过热更直接进游戏玩，前提是远端bundle全部下载完成过
        //         ProcedureMgr.RunProcedure<ProcedureLoadDll>();
        //         break;
        // }
    }
    
    private void OnHelperDownloadProgress(float arg1, long arg2, long arg3)
    {
        Log.Info("资源下载中");
        _loginPanel.SetTip($"资源下载中...({(arg2 / MB):F2}/{(arg3 / MB):F2})", 0.4f + 0.4f * arg1);
    }
}