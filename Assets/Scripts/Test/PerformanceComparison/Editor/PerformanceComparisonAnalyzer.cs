/*--------------------------------------------------------------
 * File: Test4.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/19 14:05:54 
 *--------------------------------------------------------------
 */

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class PerformanceComparisonAnalyzer : EditorWindow
{
    [MenuItem("Tools/性能对比分析")]
    public static void ShowWindow()
    {
        GetWindow<PerformanceComparisonAnalyzer>("性能对比分析");
    }
    
    private string originalReportPath = "Assets/Scripts/Test/PerformanceComparison/Original_Performance_Report.txt";
    private string optimizedReportPath = "Assets/Scripts/Test/PerformanceComparison/Optimized_Performance_Report.txt";
    private ComparisonResult comparisonResult;
    
    void OnGUI()
    {
        GUILayout.Label("性能对比分析", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("运行原始版本测试", GUILayout.Height(30)))
        {
            RunTest(false);
        }
        
        if (GUILayout.Button("运行优化版本测试", GUILayout.Height(30)))
        {
            RunTest(true);
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("对比分析结果", GUILayout.Height(40)))
        {
            CompareResults();
        }
        
        if (comparisonResult != null)
        {
            EditorGUILayout.Space();
            DisplayComparisonResults();
        }
    }
    
    void RunTest(bool useOptimized)
    {
        // 这里可以触发实际测试运行
        EditorUtility.DisplayDialog("提示", 
            $"开始运行{(useOptimized ? "优化" : "原始")}版本测试\n请查看控制台输出", 
            "确定");
    }
    
    void CompareResults()
    {
        if (!File.Exists(originalReportPath) || !File.Exists(optimizedReportPath))
        {
            EditorUtility.DisplayDialog("错误", "找不到测试结果文件", "确定");
            return;
        }
        
        comparisonResult = new ComparisonResult();
        
        // 解析原始版本结果
        string originalText = File.ReadAllText(originalReportPath);
        ParseResults(originalText, false);
        
        // 解析优化版本结果
        string optimizedText = File.ReadAllText(optimizedReportPath);
        ParseResults(optimizedText, true);
        
        // 计算改进百分比
        comparisonResult.cpuImprovement = 
            (comparisonResult.originalCpuTime - comparisonResult.optimizedCpuTime) 
            / comparisonResult.originalCpuTime * 100f;
        
        comparisonResult.gcImprovement = 
            (comparisonResult.originalGcPerMinute - comparisonResult.optimizedGcPerMinute) 
            / comparisonResult.originalGcPerMinute * 100f;
        
        // 生成HTML报告
        GenerateHTMLReport();
    }
    
    void ParseResults(string text, bool isOptimized)
    {
        // 简单解析，实际应使用正则表达式
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("平均每帧耗时:"))
            {
                string value = line.Split(':')[1].Trim().Replace("ms", "");
                float cpuTime = float.Parse(value);
                
                if (isOptimized)
                    comparisonResult.optimizedCpuTime = cpuTime;
                else
                    comparisonResult.originalCpuTime = cpuTime;
            }
            else if (line.Contains("每分钟GC次数:"))
            {
                string value = line.Split(':')[1].Trim().Replace("次", "");
                float gcPerMinute = float.Parse(value);
                
                if (isOptimized)
                    comparisonResult.optimizedGcPerMinute = gcPerMinute;
                else
                    comparisonResult.originalGcPerMinute = gcPerMinute;
            }
        }
    }
    
    void DisplayComparisonResults()
    {
        EditorGUILayout.LabelField("性能对比结果", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 创建表格样式
        GUIStyle tableStyle = new GUIStyle(GUI.skin.box);
        tableStyle.padding = new RectOffset(10, 10, 10, 10);
        
        EditorGUILayout.BeginVertical(tableStyle);
        
        // 表头
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("指标", EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField("优化前", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField("优化后", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField("改进", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // CPU占用行
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("CPU平均占用", GUILayout.Width(150));
        EditorGUILayout.LabelField($"{comparisonResult.originalCpuTime:F1}ms/frame", GUILayout.Width(100));
        EditorGUILayout.LabelField($"{comparisonResult.optimizedCpuTime:F1}ms/frame", GUILayout.Width(100));
        
        string cpuImprovementText = $"{comparisonResult.cpuImprovement:F1}%";
        Color cpuColor = comparisonResult.cpuImprovement > 0 ? Color.green : Color.red;
        GUI.color = cpuColor;
        EditorGUILayout.LabelField(cpuImprovementText, GUILayout.Width(80));
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // GC触发次数行
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("GC触发次数", GUILayout.Width(150));
        EditorGUILayout.LabelField($"{comparisonResult.originalGcPerMinute:F1}次/分钟", GUILayout.Width(100));
        EditorGUILayout.LabelField($"{comparisonResult.optimizedGcPerMinute:F1}次/分钟", GUILayout.Width(100));
        
        string gcImprovementText = $"{comparisonResult.gcImprovement:F1}%";
        Color gcColor = comparisonResult.gcImprovement > 0 ? Color.green : Color.red;
        GUI.color = gcColor;
        EditorGUILayout.LabelField(gcImprovementText, GUILayout.Width(80));
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("导出HTML报告"))
        {
            string path = EditorUtility.SaveFilePanel("保存报告", "", "性能对比报告", "html");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, comparisonResult.htmlReport);
                EditorUtility.RevealInFinder(path);
            }
        }
    }
    
    void GenerateHTMLReport()
    {
        StringBuilder html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='UTF-8'>");
        html.AppendLine("<title>性能对比报告</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
        html.AppendLine("h1 { color: #333; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 12px; text-align: center; }");
        html.AppendLine("th { background-color: #4CAF50; color: white; }");
        html.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
        html.AppendLine(".improvement-positive { color: green; font-weight: bold; }");
        html.AppendLine(".improvement-negative { color: red; font-weight: bold; }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        
        html.AppendLine("<h1>网络同步优化性能对比报告</h1>");
        html.AppendLine($"<p>生成时间: {System.DateTime.Now}</p>");
        
        html.AppendLine("<h2>性能对比结果</h2>");
        html.AppendLine("<table>");
        html.AppendLine("<tr><th>指标</th><th>优化前</th><th>优化后</th><th>改进</th></tr>");
        
        // CPU行
        html.AppendLine("<tr>");
        html.AppendLine("<td>CPU平均占用</td>");
        html.AppendLine($"<td>{comparisonResult.originalCpuTime:F1}ms/frame</td>");
        html.AppendLine($"<td>{comparisonResult.optimizedCpuTime:F1}ms/frame</td>");
        html.AppendLine($"<td class='improvement-positive'>{comparisonResult.cpuImprovement:F1}%</td>");
        html.AppendLine("</tr>");
        
        // GC行
        html.AppendLine("<tr>");
        html.AppendLine("<td>GC触发次数</td>");
        html.AppendLine($"<td>{comparisonResult.originalGcPerMinute:F1}次/分钟</td>");
        html.AppendLine($"<td>{comparisonResult.optimizedGcPerMinute:F1}次/分钟</td>");
        html.AppendLine($"<td class='improvement-positive'>{comparisonResult.gcImprovement:F1}%</td>");
        html.AppendLine("</tr>");
        
        html.AppendLine("</table>");
        
        html.AppendLine("<h2>结论</h2>");
        html.AppendLine($"<p>经过优化，CPU性能提升了<strong>{comparisonResult.cpuImprovement:F1}%</strong></p>");
        html.AppendLine($"<p>内存GC触发频率降低了<strong>{comparisonResult.gcImprovement:F1}%</strong></p>");
        
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        comparisonResult.htmlReport = html.ToString();
    }
    
    private class ComparisonResult
    {
        public float originalCpuTime;
        public float optimizedCpuTime;
        public float originalGcPerMinute;
        public float optimizedGcPerMinute;
        public float cpuImprovement;
        public float gcImprovement;
        public string htmlReport;
    }
}