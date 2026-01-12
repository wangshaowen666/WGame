/*--------------------------------------------------------------
 * File: AssetBundleTool.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/26 10:51:41 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildWindow : OdinEditorWindow
{
    [InlineEditor]
    public BuildConfig buildConfig;
    
    [MenuItem("Tools/Game/打包工具", false, 10)]
    private static void OpenWindow()
    {
        var window = GetWindow<BuildWindow>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 700);
    }
    protected override void OnEnable()
    {
        if (buildConfig ==null)
        {
            string[] guids = AssetDatabase.FindAssets("t:BuildConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                buildConfig = AssetDatabase.LoadAssetAtPath<BuildConfig>(path);
            }
            else
            {
                // 创建默认配置文件
                buildConfig = CreateInstance<BuildConfig>();
                AssetDatabase.CreateAsset(buildConfig, "Assets/Res/Config/BuildConfig.asset");
                AssetDatabase.SaveAssets();
            }
        }
    }
    
    [Title("Log Info")]
    [HideLabel]
    [MultiLineProperty(lines:6)]
    public string logInfoTextField;
    
    [ButtonGroup]
    [Button("导表")]
    public void ImportExcel()
    {
        ShellUtil.Run(buildConfig.excelShellPath);
        AutoCompleteDataTableCtrProperties();
    }
    
    [ButtonGroup]
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

    [ButtonGroup]
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

    [ButtonGroup]
    [Button("构建安装包")]
    public void BuildPackage()
    {
        try
        {
            // 检查编译状态
            if (EditorApplication.isCompiling)
            {
                AddLogInfo("错误：代码正在编译中，请等待编译完成后再进行构建");
                return;
            }
            
            // 获取项目根路径
            string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName;
            
            // 获取当前构建目标
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            
            // 根据不同平台生成合适的输出文件名
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
            
            // 构建完整的输出路径
            string outputPath = Path.Combine(projectRootPath, "Builds", buildTarget.ToString(), outputFileName);
            
            // 确保输出目录存在
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
            AddLogInfo($"开始构建安装包，目标平台：{buildTarget}");
            AddLogInfo($"输出路径：{outputPath}");
            
            // 设置构建选项
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
            
            // 执行构建
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            
            // 检查构建结果
            if (report.summary.result == BuildResult.Succeeded)
            {
                AddLogInfo($"安装包构建成功！总耗时：{report.summary.totalTime.TotalSeconds:F2}秒");
                
                // 打开构建输出目录
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
    
    [ButtonGroup]
    [Button("清除日志")]
    public void ClearLog()
    {
        logInfoTextField = "";
    }

    private void AddLogInfo(string str)
    {
        logInfoTextField = (str + "\n") + logInfoTextField;
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

        // 创建更新组
        ContentUpdateScript.CreateContentUpdateGroup(settings, modifiedEntries, "ContentUpdate_Group");
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
        string source = Path.Combine(buildConfig.dllSourcePath, EditorUserBuildSettings.activeBuildTarget.ToString());
        foreach (var dll in buildConfig.dlls)
        {
            FileUtil.CopyFile(Path.Combine(source, dll), Path.Combine(buildConfig.dllTargetPath, dll + ".bytes"));
            AddLogInfo("同步dll：" + dll);
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
        string remoteTarget = Path.Combine(buildConfig.remotePath, EditorUserBuildSettings.activeBuildTarget.ToString());

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
    
    /// <summary>
    /// 自动为DataTableCtr补全所有可获取的表的属性
    /// </summary>
    private void AutoCompleteDataTableCtrProperties()
    {
        try
        {
            while (EditorApplication.isCompiling)
            {
                Thread.Sleep(100);
            }
            
            AddLogInfo("开始自动补全DataTableCtr表属性...");
            if (!File.Exists(buildConfig.tablesFilePath))
            {
                AddLogInfo($"错误：Tables.cs文件不存在：{buildConfig.tablesFilePath}");
                return;
            }
            
            if (!File.Exists(buildConfig.dataTableCtrFilePath))
            {
                AddLogInfo($"错误：DataTableCtr.cs文件不存在：{buildConfig.dataTableCtrFilePath}");
                return;
            }
            
            string tablesContent = File.ReadAllText(buildConfig.tablesFilePath);
            Regex tablePropertyRegex = new Regex(@"^\s*public\s+(\w+)\s+(\w+)\s+\{get;\s+\}\s*$", RegexOptions.Multiline);
            List<Tuple<string, string>> tableProperties = new List<Tuple<string, string>>();
            
            foreach (Match match in tablePropertyRegex.Matches(tablesContent))
            {
                string typeName = match.Groups[1].Value;
                string propertyName = match.Groups[2].Value;
                tableProperties.Add(new Tuple<string, string>(typeName, propertyName));
            }
            
            string dataTableCtrContent = File.ReadAllText(buildConfig.dataTableCtrFilePath);
            Regex existingPropertyRegex = new Regex(@"^\s*public\s+(\w+)\s+(\w+)\s+=>\s+_tables\.(\w+);\s*$", RegexOptions.Multiline);
            HashSet<string> existingProperties = new HashSet<string>();
            
            foreach (Match match in existingPropertyRegex.Matches(dataTableCtrContent))
            {
                string propertyName = match.Groups[3].Value;
                existingProperties.Add(propertyName);
            }
            
            int insertIndex = dataTableCtrContent.IndexOf("private cfg.Tables _tables;");
            if (insertIndex == -1)
            {
                AddLogInfo("错误：在DataTableCtr.cs中未找到private cfg.Tables _tables;声明");
                return;
            }
            
            insertIndex += "private cfg.Tables _tables;".Length;
            
            // 构建新的属性字符串
            StringBuilder newPropertiesBuilder = new StringBuilder();
            int newPropertyCount = 0;
            
            foreach (var tableProperty in tableProperties)
            {
                string typeName = tableProperty.Item1;
                string propertyName = tableProperty.Item2;
                
                if (!existingProperties.Contains(propertyName))
                {
                    newPropertiesBuilder.AppendLine($"    public {typeName} {propertyName} => _tables.{propertyName};");
                    newPropertyCount++;
                }
            }
            
            if (newPropertyCount > 0)
            {
                if (insertIndex < dataTableCtrContent.Length && dataTableCtrContent[insertIndex] != '\n')
                {
                    newPropertiesBuilder.Insert(0, "\n");
                }
                
                if (insertIndex > 0 && dataTableCtrContent[insertIndex - 1] != '\n')
                {
                    newPropertiesBuilder.Insert(0, "\n");
                }
                dataTableCtrContent = dataTableCtrContent.Insert(insertIndex, newPropertiesBuilder.ToString());
                File.WriteAllText(buildConfig.dataTableCtrFilePath, dataTableCtrContent);
                AssetDatabase.Refresh();
                AddLogInfo($"已成功为DataTableCtr补全{newPropertyCount}个表属性");
            }
            else
            {
                AddLogInfo("DataTableCtr已有所有表属性，无需补全");
            }
        }
        catch (Exception ex)
        {
            AddLogInfo($"自动补全DataTableCtr表属性失败：{ex.Message}");
            Debug.LogError($"自动补全DataTableCtr表属性失败：{ex}");
        }
    }
}