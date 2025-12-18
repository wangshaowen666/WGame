/*--------------------------------------------------------------
 * File: ProcudureResCheck.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/11 14:09:47 
 *--------------------------------------------------------------
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public class ProcedureResCheck_AA : ProcedureBase
{
    private AddressableHelper _helper;
    private CancellationTokenSource _tokenSource;

    public override void OnInit(Fsm fsm)
    {
        base.OnInit(fsm);
        _helper = new AddressableHelper();
        _helper.OnLoadFail += HelperOnLoadFail;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        
        _tokenSource = new CancellationTokenSource();
        AsyncRun().Forget();
    }

    private async UniTaskVoid AsyncRun()
    {
        // 协作式取消，异步编程中官方推荐做法。
        try
        {
            await _helper.InitAsync();
            await _helper.UpdateCatalog(_tokenSource.Token);
            await _helper.CheckRes(_tokenSource.Token);
            await _helper.Download(_tokenSource.Token);
            
            Procedure.Instance.RunProcedure<ProcedurePreload>();
        }
        catch (OperationCanceledException e)
        {
            Log.Info("任务出错，被取消");
        }
        catch (Exception e)
        {
            Log.Error("更新执行出错:" + e.Message);
        }
    }
    
    private void HelperOnLoadFail(int result)
    {
        switch (result)
        {
            
        }
        Log.Info("执行加载出错回调");
        // 它不会真的取消任务，只会设置可取消标志
        _tokenSource.Cancel();
    }
}