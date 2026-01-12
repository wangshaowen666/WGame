/*--------------------------------------------------------------
 * File: BuildConfig.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/12 11:00:00 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "BuildConfig", menuName = "Game/BuildConfig")]
public class BuildConfig : ScriptableObject
{
    [Title("热更C#的dll集合")]
    public List<string> dlls = new List<string>();
    
    [Title("服务器路径")]
    [FolderPath(AbsolutePath = true)]
    public string remotePath = "";
    
    [Title("热更dll源目录")]
    [FolderPath(AbsolutePath = true)]
    public string dllSourcePath = "";
    
    [Title("热更dll目标目录")]
    [FolderPath(AbsolutePath = true)]
    public string dllTargetPath = "";
    
    [Title("excel导表工具路径")]
    [FilePath(AbsolutePath = true)]
    public string excelShellPath = "";
    
    [Title("Table类路径")]
    [FilePath(AbsolutePath = true)]
    public string tablesFilePath = "";
    
    [Title("DataTableCtr类路径")]
    [FilePath(AbsolutePath = true)]
    public string dataTableCtrFilePath = "";
}
