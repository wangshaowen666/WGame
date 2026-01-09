/*--------------------------------------------------------------
 * File: AssetBundleTool.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/26 10:51:41 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public class AssetBundleWindow : OdinEditorWindow
{
    [MenuItem("Tools/Odin/AssetBundle处理", false, 10)]
    private static void OpenWindow()
    {
        var window = GetWindow<AssetBundleWindow>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 700);
    }

    [FolderPath(AbsolutePath = true)]
    public string remotePath = "/Library/WebServer/Documents/MyServer";
    
    [FolderPath(AbsolutePath = true)]
    public string dllSourcePath = "/Users/wangshaowen/wgame/HybridCLRData/HotUpdateDlls";
    
    [FolderPath(AbsolutePath = true)]
    public string dllTargetPath = "/Users/wangshaowen/wgame/Assets/Res/Dll";

    [Sirenix.OdinInspector.FilePath(AbsolutePath = true)]
    public string excelShellPath = "/Users/wangshaowen/wgame/DataTables/gen.sh";

    public bool clearBeforeCopy = true;
    
    [Title("Log Info")]
    [HideLabel]
    [MultiLineProperty(lines:6)]
    public string logInfoTextField;
    
    [ButtonGroup]
    [Button("清理无效Bundle", ButtonSizes.Large)]
    public void ClearInvalidBundle()
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

    [ButtonGroup]
    [Button("拷贝Bundle")]
    public void CopyBundle()
    {
        string absolutePath = GetBundleBuildPath();
        string remoteTarget = Path.Combine(remotePath, EditorUserBuildSettings.activeBuildTarget.ToString());

        if (clearBeforeCopy)
        {
            Directory.Delete(remoteTarget, true);
        }
        
        FileUtil.CopyDirectory(absolutePath, remoteTarget);
        AddLogInfo("拷贝完成");
    }
    
    [ButtonGroup]
    [Button("清除并拷贝")]
    [GUIColor(0, 1, 0)]
    public void ClearAndCopyBundle()
    {
        ClearInvalidBundle();
        CopyBundle();
    }
    
    [ButtonGroup]
    [Button("同步dll")]
    public void CopyDll()
    {
        string source = Path.Combine(dllSourcePath, EditorUserBuildSettings.activeBuildTarget.ToString());
        List<string> dlls = new List<string> { "Game.dll", "Framework.JIT.dll" };
        foreach (var dll in dlls)
        {
            FileUtil.CopyFile(Path.Combine(source, dll), Path.Combine(dllTargetPath, dll + ".bytes"));
            AddLogInfo("同步dll：" + dll);
        }
        AssetDatabase.Refresh();
    }
    
    [ButtonGroup]
    [Button("导表")]
    public void ImportExcel()
    {
        ShellUtil.Run(excelShellPath);
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