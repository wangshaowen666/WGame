/*--------------------------------------------------------------
 * File: ToolBox.Excel.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/19 11:26:43 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public partial class ToolBox
{
    [TitleGroup("导表工具")]
    [HorizontalGroup("导表工具/水平布局", Width = 80)] 
    [ButtonGroup("导表工具/水平布局/导表")]
    [Button("导表", 30)]
    public void ImportExcel()
    {
        ShellUtil.Run(editorPathConfig.excelShellPath);
        AddLogInfo("导表完成");
        AssetDatabase.Refresh();
        AutoCompleteDataTableCtrProperties();
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