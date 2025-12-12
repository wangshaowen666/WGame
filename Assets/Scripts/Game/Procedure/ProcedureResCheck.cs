/*--------------------------------------------------------------
 * File: ProcudureResCheck.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/11 14:09:47 
 *--------------------------------------------------------------
 */

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ProcedureResCheck : ProcedureBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        
        // todo 先用最笨的方式实现，后续再想抽象的事情
        AsyncRun().Forget();
    }

    private async UniTaskVoid AsyncRun()
    {
        await Addressables.InitializeAsync();

        int flag = await UpdateCatalog();
        if (flag < 0) return;

        await CheckRes(flag);
    }

    private async UniTask<int> UpdateCatalog() 
    {
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        await checkHandle;
        if (checkHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Log.Error("Catalog 检查失败");
            Addressables.Release(checkHandle);
            return -1;
        }

        int ret = 0;
        if (checkHandle.Result != null && checkHandle.Result.Count > 0)
        {
            Log.Info("需要更新catalog");
            var updateHandle = Addressables.UpdateCatalogs(true, checkHandle.Result, false);
            await updateHandle;
            if (updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Log.Error("Catalog 更新失败");
                ret = -2;
            }
            else
            {
                ret = 1;
            }
            
            Addressables.Release(checkHandle);
        }

        Addressables.Release(checkHandle);
        return ret;
    }

    private async UniTask<int> CheckRes(int flag)
    {
        if (flag == 0 && PlayerPrefsUtil.GetInt("UpdateFlag") == 1)
            return 0;

        return 1;
    }
    
}