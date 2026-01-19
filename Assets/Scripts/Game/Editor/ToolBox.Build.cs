/*--------------------------------------------------------------
 * File: ToolBox.Build.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/19 11:07:23 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HybridCLR.Editor.Settings;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

public partial class ToolBox
{
    [TitleGroup("打包工具")]
    [HorizontalGroup("打包工具/水平布局", width:100)]
    [ButtonGroup("打包工具/水平布局/打包")]
    [Button("构建热更Bundle", 30)]
    [GUIColor(0, 1, 0)]
    public void UpdateBuild()
    {
        EditorApplication.ExecuteMenuItem("HybridCLR/CompileDll/ActiveBuildTarget");
        AddLogInfo("已调用HybridCLR ActiveBuildTarget编译");
        
        CopyDll();
       
        BuildContentUpdate();
        
        ClearInvalidBundle();
        
        CopyBundle();
    }

    [ButtonGroup("打包工具/水平布局/打包")]
    [Button("构建新Bundle")]
    public void NewBuild()
    {
        EditorApplication.ExecuteMenuItem("HybridCLR/Generate/All");
        AddLogInfo("已调用HybridCLR Generate/All编译");
        
        CopyDll();
       
        BuildNewContent();
        
        ClearInvalidBundle();
        
        CopyBundle();
    }

    [ButtonGroup("打包工具/水平布局/打包")]
    [Button("构建安装包")]
    public void BuildPackage()
    {
        try
        {
            if (EditorApplication.isCompiling)
            {
                AddLogInfo("错误：代码正在编译中，请等待编译完成后再进行构建");
                return;
            }
            
            string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName;
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            
            string outputFileName = "Game";
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                    outputFileName += ".exe";
                    break;
                case BuildTarget.StandaloneWindows:
                    outputFileName += ".exe";
                    break;
                case BuildTarget.StandaloneOSX:
                    outputFileName += ".app";
                    break;
                case BuildTarget.Android:
                    outputFileName += ".apk";
                    break;
                case BuildTarget.iOS:
                    // iOS构建输出到文件夹
                    outputFileName += "_iOS";
                    break;
                default:
                    outputFileName += "_" + buildTarget.ToString();
                    break;
            }
            
            string outputPath = Path.Combine(projectRootPath, "Builds", buildTarget.ToString(), outputFileName);
            
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
            AddLogInfo($"开始构建安装包，目标平台：{buildTarget}");
            AddLogInfo($"输出路径：{outputPath}");
            
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray(),
                locationPathName = outputPath,
                target = buildTarget,
                options = BuildOptions.None
            };
            
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            
            if (report.summary.result == BuildResult.Succeeded)
            {
                AddLogInfo($"安装包构建成功！总耗时：{report.summary.totalTime.TotalSeconds:F2}秒");
                
                string buildDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(buildDir) && Directory.Exists(buildDir))
                {
                    EditorUtility.RevealInFinder(buildDir);
                }
            }
            else
            {
                AddLogInfo("错误：安装包构建失败");
            }
        }
        catch (Exception ex)
        {
            AddLogInfo($"构建安装包时发生异常：{ex.Message}");
            Debug.LogError($"构建安装包失败：{ex}");
        }
    }
    
    
    /// <summary>
    /// 构建新包
    /// </summary>
    private void BuildNewContent()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            AddLogInfo("Addressable设置未找到");
            return;
        }
        
        AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
        AddressableAssetSettings.BuildPlayerContent(out var result);
        
        if (string.IsNullOrEmpty(result.Error))
        {
            AddLogInfo("新构建成功");
        }
        else
        {
            AddLogInfo($"新构建失败，结果：{result.Error}");
        }
    }

    /// <summary>
    /// 构建热更包
    /// </summary>
    private void BuildContentUpdate()
    {
        string stateBinPath = $"Assets/AddressableAssetsData/{EditorUserBuildSettings.activeBuildTarget.ToString()}/addressables_content_state.bin";
        if (!File.Exists(stateBinPath))
        {
            AddLogInfo($"State文件不存在: {stateBinPath}");
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            AddLogInfo("Addressable设置未找到");
            return;
        }
        
        // 收集修改的资源(远程资源不包含)
        var modifiedEntries = ContentUpdateScript.GatherModifiedEntries(settings, stateBinPath);
        if (modifiedEntries != null && modifiedEntries.Count > 0)
        {
            ContentUpdateScript.CreateContentUpdateGroup(settings, modifiedEntries, "ContentUpdate_Group");
        }
      
        var result = ContentUpdateScript.BuildContentUpdate(settings, stateBinPath);
        if (string.IsNullOrEmpty(result.Error))
        {
            AddLogInfo($"热更构建成功");
        }
        else
        {
            AddLogInfo($"构建失败: {result.Error}");
        }
    }
    
    /// <summary>
    /// 同步华佗热更的脚本dll
    /// </summary>
    private void CopyDll()
    {
        string source = Path.Combine(editorPathConfig.dllSourcePath, EditorUserBuildSettings.activeBuildTarget.ToString());
        
        var settings = HybridCLRSettings.Instance;
        var hotUpdateAssemblies = settings.hotUpdateAssemblyDefinitions;
        foreach (var assemblyName in hotUpdateAssemblies)
        {
            FileUtil.CopyFile(Path.Combine(source, assemblyName.name + ".dll"), Path.Combine(editorPathConfig.dllTargetPath, assemblyName.name + ".dll.bytes"));
            AddLogInfo("同步dll：" + assemblyName.name + ".dll");
        }
        AssetDatabase.Refresh();
    }
    
    /// <summary>
    /// 清除无效的bundle
    /// </summary>
    private void ClearInvalidBundle()
    {
        string absolutePath = GetBundleBuildPath();
        string version = PlayerSettings.bundleVersion;
        string catalogPath = Path.Combine(absolutePath, $"catalog/catalog_{version}.json");

        var content = File.ReadAllText(catalogPath);
        Dictionary<string, object> dict = ParseUtil.DeJson(content) as Dictionary<string, object>;
        List<object> fileNms = dict["m_InternalIds"] as List<object>;
        
        List<string> remoteBundleNames = fileNms
            .Select(f => f.ToString())
            .Where(f => Path.GetFileName(f).EndsWith(".bundle") && Path.GetFileName(f).StartsWith("remote_"))
            .Select(f => Path.GetFileName(f))
            .ToList();
        
        
        string[] allFiles = Directory.GetFiles(absolutePath, "*", SearchOption.TopDirectoryOnly);
        foreach (var nm in allFiles)
        {
            var fNm = Path.GetFileName(nm);
            if (fNm.EndsWith(".bundle") && !remoteBundleNames.Contains(fNm))
            {
                AddLogInfo("删除文件:" + nm);
                File.Delete(nm);
            }
        }
        AddLogInfo("清除无效Bundle完成");
    }
    
    /// <summary>
    /// 将本地bundle拷贝到服务器
    /// </summary>
    private void CopyBundle()
    {
        string absolutePath = GetBundleBuildPath();
        string remoteTarget = Path.Combine(editorPathConfig.remotePath, EditorUserBuildSettings.activeBuildTarget.ToString());

        Directory.Delete(remoteTarget, true);
        FileUtil.CopyDirectory(absolutePath, remoteTarget);
        AddLogInfo("拷贝完成");
    }

    /// <summary>
    /// 获取bundle构建路径
    /// </summary>
    /// <returns></returns>
    private string GetBundleBuildPath()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        string activeProfileId = settings.activeProfileId;
        string remoteBuildPath = settings.profileSettings.GetValueByName(activeProfileId, "Remote.BuildPath");
        string resolvedPath = remoteBuildPath.Replace("[BuildTarget]", EditorUserBuildSettings.activeBuildTarget.ToString());
        string absolutePath = Path.GetFullPath(resolvedPath);
        
        return absolutePath;
    }
}