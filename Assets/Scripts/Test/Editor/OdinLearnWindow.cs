/*--------------------------------------------------------------
 * File: OdinTestWindow.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/26 11:20:17 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using FilePath = Sirenix.OdinInspector.FilePathAttribute;
public class OdinLearnWindow : OdinEditorWindow
{
    public enum BuildMode
    {
        NewBuild,
        UpdateBuild
    }
    
    [MenuItem("Tools/Odin/API学习", false, 1)]
    private static void OpenWindow()
    {
        var window = GetWindow<OdinLearnWindow>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 700);
    }

    [GUIColor(0.3f, 0.8f, 0.8f, 1f)]
    public int ColoredInt1;
    
    [ButtonGroup]
    [GUIColor(0, 1, 0)]
    private void Apply()
    {
        Log.Info("点击Apply", mode2);
    }

    
    [ButtonGroup]
    [Button(ButtonSizes.Large)]
    [GUIColor(1, 0.6f, 0.4f)]
    private void Cancel()
    {
        Log.Info("点击Cancel");
    }
    
    [Title("Wide Multiline Text Field")]
    [HideLabel]
    [MultiLineProperty]
    public string WideMultilineTextField = "";
    
    public BuildMode mode = BuildMode.NewBuild;
    
    [EnumToggleButtons]
    public BuildMode mode2 = BuildMode.NewBuild;

    public Dictionary<string, List<string>> dict;
    
    [ProgressBar(0, 100)]
    public int ProgressBar = 50;
    
    [ProgressBar(0, 10, 0, 1, 0, Segmented = true)]
    public int SegmentedColoredBar = 5;
    
    [AssetSelector]
    public Material material;

    [FilePath]
    public string filePath;
    
    [FilePath(Extensions = "cs")]
    [BoxGroup("Conditions")]
    public string ScriptFiles;

    [SerializeField]
    [FolderPath(AbsolutePath = true)]
    [BoxGroup("Conditions")]
    public string AbsolutePath;

}

