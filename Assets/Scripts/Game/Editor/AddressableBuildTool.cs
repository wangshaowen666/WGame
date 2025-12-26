/*--------------------------------------------------------------
 * File: AddressableBuildTool.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/23 10:50:23 
 *--------------------------------------------------------------
 */

using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class AddressableBuildTool : EditorWindow
{
    private string localServerPath = "";
    private BuildMode selectedBuildMode = BuildMode.NewBuild;
    private bool clearCacheBeforeBuild = true;
    private bool copyToServerAfterBuild = true;

    private enum BuildMode
    {
        NewBuild,
        UpdateBuild
    }

    [MenuItem("Tools/Addressable Build Tool")]
    public static void ShowWindow()
    {
        GetWindow<AddressableBuildTool>("Addressable Build Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Addressable打包工具", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        // 构建模式选择
        selectedBuildMode = (BuildMode)EditorGUILayout.EnumPopup("构建模式", selectedBuildMode);
        
        // 服务器路径设置
        EditorGUILayout.BeginHorizontal();
        localServerPath = EditorGUILayout.TextField("服务器目录", localServerPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            localServerPath = EditorUtility.SaveFolderPanel("选择服务器目录", "", "");
        }
        EditorGUILayout.EndHorizontal();
        
        // 选项设置
        clearCacheBeforeBuild = EditorGUILayout.Toggle("构建前清理缓存", clearCacheBeforeBuild);
        copyToServerAfterBuild = EditorGUILayout.Toggle("构建后复制到服务器", copyToServerAfterBuild);
        
        EditorGUILayout.Space();
        
        // 操作按钮
        if (GUILayout.Button("执行构建", GUILayout.Height(30)))
        {
            ExecuteBuildPipeline();
        }
        
        EditorGUILayout.Space();
        
        // 快捷操作
        if (GUILayout.Button("仅清理缓存"))
        {
            ClearAllCaches();
        }
    }

    public void ExecuteBuildPipeline()
    {
        try
        {
            // 1. 清理缓存
            if (clearCacheBeforeBuild)
            {
                ClearAllCaches();
            }

            // 2. 执行Addressable构建
            bool buildSuccess = ExecuteAddressableBuild();
            if (!buildSuccess)
            {
                EditorUtility.DisplayDialog("构建失败", "Addressable构建失败，请检查错误日志", "确定");
                return;
            }

            // 3. 复制到服务器目录
            if (copyToServerAfterBuild && !string.IsNullOrEmpty(localServerPath))
            {
                //CopyToServerDirectory();
            }

            EditorUtility.DisplayDialog("完成", "打包流程执行完毕！", "确定");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"打包流程异常: {e.Message}");
            EditorUtility.DisplayDialog("错误", $"执行过程中发生错误: {e.Message}", "确定");
        }
    }

    private void ClearAllCaches()
    {
        try
        {
            // 清理Addressable构建缓存
            AddressableAssetSettings.CleanPlayerContent();
            
            // 清理本地运行时缓存目录
            ClearRuntimeCache();
            
            Debug.Log("缓存清理完成");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"缓存清理过程中出现警告: {e.Message}");
        }
    }

    private void ClearRuntimeCache()
    {
        // 获取缓存路径（Windows示例）
        string cachePath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), 
            "Low", 
            PlayerSettings.companyName, 
            PlayerSettings.productName, 
            "com.unity.addressables"
        );

        if (Directory.Exists(cachePath))
        {
            Directory.Delete(cachePath, true);
            Debug.Log($"已清理运行时缓存: {cachePath}");
        }
    }

    private bool ExecuteAddressableBuild()
    {
        try
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable设置未找到！");
                return false;
            }

            switch (selectedBuildMode)
            {
                case BuildMode.NewBuild:
                    AddressableAssetSettings.BuildPlayerContent();
                    break;
                    
                // case BuildMode.UpdateBuild:
                //     // 增量构建需要选择之前的addressables_content_state.bin文件
                //     string statePath = EditorUtility.OpenFilePanel(
                //         "选择addressables_content_state.bin文件", 
                //         "Assets/AddressableAssetsData", 
                //         "bin"
                //     );
                //     
                //     if (!string.IsNullOrEmpty(statePath))
                //     {
                //         AddressableAssetSettings.BuildPlayerContent(out var result, );
                //         // 3. 配置构建参数，关键是指定 `PreviousBuildStatePath`
                //         AddressableAssetBuildContext buildContext = new AddressableAssetBuildContext
                //         {
                //             Settings = settings,
                //             PreviousBuildStatePath = previousStateFilePath // 核心：指定增量基准
                //         };
                //
                //         // 4. 执行构建
                //         try
                //         {
                //             // 此方法会触发增量构建流程
                //             AddressableAssetSettings.BuildPlayerContent(buildContext);
                //             EditorUtility.DisplayDialog("成功", "增量包构建完成！", "确定");
                //         }
                //         catch (Exception e)
                //         {
                //             EditorUtility.DisplayDialog("错误", $"增量包构建失败：{e.Message}", "确定");
                //         }
                //         
                //         
                //         var buildResult = BuildPipeline.BuildAddressables(statePath, false);
                //
                //         if (buildResult == 0) // 0 通常表示成功
                //         {
                //             Debug.Log("✅ Addressables 增量包构建成功！");
                //         }
                //         else
                //         {
                //             Debug.LogError($"❌ Addressables 增量包构建失败，错误码：{buildResult}");
                //         }
                //         //AddressableAssetSettings.BuildPlayerContent(statePath);
                //     }
                //     else
                //     {
                //         Debug.LogWarning("未选择state文件，使用全新构建");
                //         //AddressableAssetSettings.BuildPlayerContent();
                //     }
                //     break;
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Addressable构建失败: {e.Message}");
            return false;
        }
    }

    private void CopyToServerDirectory()
    {
        try
        {
            // var settings = AddressableAssetSettingsDefaultObject.Settings;
            // string remoteBuildPath = settings.profileSettings.GetValueByName(
            //     settings.activeProfile, 
            //     "RemoteBuildPath"
            // );

            // 解析路径中的变量（如[BuildTarget]）
            // remoteBuildPath = remoteBuildPath.Replace(
            //     "[BuildTarget]", 
            //     EditorUserBuildSettings.activeBuildTarget.ToString()
            // );
            //
            // if (Directory.Exists(remoteBuildPath))
            // {
            //     // 确保目标目录存在
            //     if (!Directory.Exists(localServerPath))
            //     {
            //         Directory.CreateDirectory(localServerPath);
            //     }
            //
            //     // 清空目标目录（可选）
            //     foreach (var file in Directory.GetFiles(localServerPath))
            //     {
            //         File.Delete(file);
            //     }
            //
            //     // 复制所有文件
            //     foreach (var file in Directory.GetFiles(remoteBuildPath))
            //     {
            //         string destFile = Path.Combine(localServerPath, Path.GetFileName(file));
            //         File.Copy(file, destFile, true);
            //     }
            //
            //     Debug.Log($"已复制文件到服务器目录: {localServerPath}");
            // }
            // else
            // {
            //     Debug.LogWarning($"远程构建路径不存在: {remoteBuildPath}");
            // }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"复制到服务器目录失败: {e.Message}");
        }
    }
}