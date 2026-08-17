/*--------------------------------------------------------------
 * File: ToolBox.Excel.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/19 11:26:43 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public partial class ToolBox
{
    [TitleGroup("导表工具")]
    [HorizontalGroup("导表工具/水平布局", Width = 80)] 
    [Button("导表", ButtonSizes.Large)]
    public void ImportExcel()
    {
        var ret = ShellUtil.Run(editorPathConfig.excelShellPath);
        if (!ret.TrimEnd().EndsWith("bye~"))
        {
            AddLogInfo("导表出错：" + ret);
            return;
        }
        AddLogInfo("导表完成");
        AssetDatabase.Refresh();
        EditorApplication.delayCall += AutoCompleteDataTableCtrProperties;
    }
    
    [HorizontalGroup("导表工具/水平布局", Width = 80)] 
    [Button("导Proto", ButtonSizes.Large)]
    public void ImportProto()
    {
        var ret = ShellUtil.Run(editorPathConfig.protoShellPath);
        if (!ret.Contains("[PROTO_OK]"))
        {
            AddLogInfo("导proto出错：" + ret);
            return;
        }
        AddLogInfo("导proto完成");
        AssetDatabase.Refresh();
    }
    
    /// <summary>
    /// 自动为DataTableCtr补全所有可获取的表的属性
    /// </summary>
    private void AutoCompleteDataTableCtrProperties()
    {
        try
        {
            AddLogInfo("开始自动补全DataTableCtr表属性...");
            var tablePath = GetScriptPathByType(typeof(cfg.Tables));
            if (!File.Exists(tablePath))
            {
                AddLogInfo($"错误：Tables.cs文件不存在");
                return;
            }
            
            var dataTableMgrPath = GetScriptPathByType(typeof(DataTableMgr));
            if (!File.Exists(dataTableMgrPath))
            {
                AddLogInfo($"错误：DataTableCtr.cs文件不存在");
                return;
            }
            
            string tablesContent = File.ReadAllText(tablePath);
            Regex tablePropertyRegex = new Regex(@"^\s*public\s+(\w+)\s+(\w+)\s+\{get;\s+\}\s*$", RegexOptions.Multiline);
            List<Tuple<string, string>> tableProperties = new List<Tuple<string, string>>();
            HashSet<string> validPropertyNames = new HashSet<string>();
            
            foreach (Match match in tablePropertyRegex.Matches(tablesContent))
            {
                string typeName = match.Groups[1].Value;
                string propertyName = match.Groups[2].Value;
                tableProperties.Add(new Tuple<string, string>(typeName, propertyName));
                validPropertyNames.Add(propertyName);
            }
            
            string dataTableCtrContent = File.ReadAllText(dataTableMgrPath);
            Regex dataTablePropertyRegex = new Regex(@"^\s*public\s+(\w+)\s+(\w+)\s+=>\s+_tables\.(\w+);\s*$", RegexOptions.Multiline);
            
            int removedCount = 0;
            dataTableCtrContent = dataTablePropertyRegex.Replace(dataTableCtrContent, match =>
            {
                string propertyName = match.Groups[2].Value;
                if (!validPropertyNames.Contains(propertyName))
                {
                    removedCount++;
                    return string.Empty;
                }
                return match.Value;
            });
            
            HashSet<string> existingProperties = new HashSet<string>();
            foreach (Match match in dataTablePropertyRegex.Matches(dataTableCtrContent))
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
            
            if (removedCount > 0 || newPropertyCount > 0)
            {
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
                }
                
                File.WriteAllText(dataTableMgrPath, dataTableCtrContent);
                AssetDatabase.Refresh();
                
                var logParts = new List<string>();
                if (removedCount > 0) logParts.Add($"移除{removedCount}个无效属性");
                if (newPropertyCount > 0) logParts.Add($"新增{newPropertyCount}个属性");
                AddLogInfo($"已成功更新DataTableCtr表属性：" + string.Join("，", logParts));
            }
            else
            {
                AddLogInfo("DataTableCtr已有所有表属性，无需更新");
            }
        }
        catch (Exception ex)
        {
            AddLogInfo($"自动补全DataTableCtr表属性失败：{ex.Message}");
            Debug.LogError($"自动补全DataTableCtr表属性失败：{ex}");
        }
    }
    
    private string GetScriptPathByType(Type type)
    {
        string[] guids = AssetDatabase.FindAssets($"t:Script {type.Name}");

        if (guids.Length == 0)
            return null;

        return AssetDatabase.GUIDToAssetPath(guids[0]);
    }
   
}