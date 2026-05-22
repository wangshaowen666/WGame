/*--------------------------------------------------------------
 * File: SceneCtr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/22 18:43:02 
 *--------------------------------------------------------------
 */

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneMgr : ManagerBase
{
    private SceneInstance _curScene;
    private bool _isLoading;

    // 先实现，后续把AA移走，面向接口实现
    public async UniTask LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (_isLoading) return;
        
        _isLoading = true;

        try
        {
            var handle = Addressables.LoadSceneAsync(sceneName, mode);
            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (mode == LoadSceneMode.Single && _curScene.Scene.IsValid())
                {
                    await Addressables.UnloadSceneAsync(_curScene);
                }
                _curScene = handle.Result;
            }
            else
            {
                Log.Error("场景加载失败");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _isLoading = false;
        }
    }
}