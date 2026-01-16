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
using HybridCLR.Editor.Settings;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Serialization;

public class ToolBox : OdinEditorWindow
{
    [Title("日志信息")]
    [HideLabel]
    [MultiLineProperty(lines:6)]
    public string logInfoTextField;
    
    [FormerlySerializedAs("buildConfig")]
    [InlineEditor]
    [Title("路径配置")]
    [HideLabel]
    public EditorPathConfig editorPathConfig;
    
    [MenuItem("Tools/Game/工具箱", false, 10)]
    private static void OpenWindow()
    {
        var window = GetWindow<ToolBox>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 700);
    }
    protected override void OnEnable()
    {
        if (editorPathConfig ==null)
        {
            string[] guids = AssetDatabase.FindAssets("t:EditorPathConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                editorPathConfig = AssetDatabase.LoadAssetAtPath<EditorPathConfig>(path);
            }
            else
            {
                // 创建默认配置文件
                editorPathConfig = CreateInstance<EditorPathConfig>();
                AssetDatabase.CreateAsset(editorPathConfig, "Assets/Res/Config/EditorPathConfig.asset");
                AssetDatabase.SaveAssets();
            }
        }
    }
    
    [TitleGroup("打包工具")]
    [ButtonGroup("打包工具/打包")]
    [Button("导表")]
    public void ImportExcel()
    {
        ShellUtil.Run(editorPathConfig.excelShellPath);
        AddLogInfo("导表完成");
        AssetDatabase.Refresh();
        AutoCompleteDataTableCtrProperties();
    }
    
    [ButtonGroup("打包工具/打包")]
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

    [ButtonGroup("打包工具/打包")]
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

    [ButtonGroup("打包工具/打包")]
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

    [ButtonGroup("打包工具/打包")]
    [Button("测试")]
    public void Test()
    {
        var a = cfg.DPnlId.MainPanel;
        AddLogInfo(a.ToString() + "   " + (int)a);
    }
    
    [ButtonGroup("打包工具/打包")]
    [Button("清除日志")]
    public void ClearLog()
    {
        logInfoTextField = "";
    }

    //[OnInspectorGUI] private void Space1() { GUILayout.Space(20); }
    
#if STATS_ON
    [TitleGroup("统计工具")]
    [ButtonGroup("统计工具/统计")]
    [Button("导出对象池统计数据", 30)]
    public void ExportObjectPoolInfo()
    {
        string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty, $"对象池统计数据 {DateTime.Now}.csv", string.Empty);
        if (!string.IsNullOrEmpty(exportFileName))
        {
            try
            {
                var contents = ObjectMgr.Instance.DealPoolStats();
                AddLogInfo(ParseUtil.ToJson(contents));
                int index = 0;
                string[] data = new string[contents.Count + 1];
                data[index++] = "类名,变量名,总对象数,活跃对象数,非活跃对象数,峰值对象数,总获取次数,总放回次数,总释放次数,总加载次数,外部对象数";
                foreach (string str in contents)
                {
                    data[index++] = str;
                }

                File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                AddLogInfo(string.Format("导出对象池统计信息 '{0}' 成功.", exportFileName));
            }
            catch (Exception exception)
            {
                AddLogInfo(string.Format("导出对象池统计信息 '{0}' 失败, 原因'{1}'.", exportFileName, exception));
            }
        }
    }
    
    [ButtonGroup("统计工具/统计")]
    [Button("导出类池统计数据")]
    public void ExportClassPoolInfo()
    {
        string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty, $"类池统计数据 {DateTime.Now}.csv", string.Empty);
        if (!string.IsNullOrEmpty(exportFileName))
        {
            try
            {
                var contents = ClassPool.DealPoolStats();
                AddLogInfo(ParseUtil.ToJson(contents));
                int index = 0;
                string[] data = new string[contents.Count + 1];
                data[index++] = "类名,容量,预热数量,创建数量,池中总数量,峰值数量,总获取次数,总放回次数,总释放数量";
                foreach (string str in contents)
                {
                    data[index++] = str;
                }

                File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                AddLogInfo(string.Format("导出类池统计信息 '{0}' 成功.", exportFileName));
            }
            catch (Exception exception)
            {
                AddLogInfo(string.Format("导出类池统计信息 '{0}' 失败, 原因'{1}'.", exportFileName, exception));
            }
        }
    }
#endif

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
            if (!File.Exists(editorPathConfig.tablesFilePath))
            {
                AddLogInfo($"错误：Tables.cs文件不存在：{editorPathConfig.tablesFilePath}");
                return;
            }
            
            if (!File.Exists(editorPathConfig.dataTableCtrFilePath))
            {
                AddLogInfo($"错误：DataTableCtr.cs文件不存在：{editorPathConfig.dataTableCtrFilePath}");
                return;
            }
            
            string tablesContent = File.ReadAllText(editorPathConfig.tablesFilePath);
            Regex tablePropertyRegex = new Regex(@"^\s*public\s+(\w+)\s+(\w+)\s+\{get;\s+\}\s*$", RegexOptions.Multiline);
            List<Tuple<string, string>> tableProperties = new List<Tuple<string, string>>();
            
            foreach (Match match in tablePropertyRegex.Matches(tablesContent))
            {
                string typeName = match.Groups[1].Value;
                string propertyName = match.Groups[2].Value;
                tableProperties.Add(new Tuple<string, string>(typeName, propertyName));
            }
            
            string dataTableCtrContent = File.ReadAllText(editorPathConfig.dataTableCtrFilePath);
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
                File.WriteAllText(editorPathConfig.dataTableCtrFilePath, dataTableCtrContent);
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