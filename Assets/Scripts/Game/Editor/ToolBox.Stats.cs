/*--------------------------------------------------------------
 * File: ToolBox.Stats.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/19 10:26:20 
 *--------------------------------------------------------------
 */

using System;
using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using UnityEditor;

public partial class ToolBox
{
#if STATS_ON && UNITY_EDITOR
    [TitleGroup("统计工具")]
    [HorizontalGroup("统计工具/水平布局", Width = 130)]
    [Button("导出对象池统计数据", ButtonSizes.Large)]
    public void ExportObjectPoolInfo()
    {
        string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty, $"对象池统计数据 {DateTime.Now}.csv", string.Empty);
        if (!string.IsNullOrEmpty(exportFileName))
        {
            try
            {
                var contents = FrameworkMgr.ObjectPool.DealPoolStats();
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
    
    [HorizontalGroup("统计工具/水平布局", Width = 120, PaddingLeft = 0)]
    [Button("导出类池统计数据", ButtonSizes.Large)]
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
    
    [HorizontalGroup("统计工具/水平布局", Width = 160)]
    [Button("导出AssetBundle统计数据", ButtonSizes.Large)]
    public void ExportAssetBundleInfo()
    {
        string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty, $"AssetBundle统计数据 {DateTime.Now}.csv", string.Empty);
        if (!string.IsNullOrEmpty(exportFileName))
        {
            try
            {
                var contents = FrameworkMgr.Res.DealPoolStats();
                AddLogInfo(ParseUtil.ToJson(contents));
                int index = 0;
                string[] data = new string[contents.Count + 1];
                data[index++] = "资源名,当前引用数量,峰值引用数量,总获取次数,总释放次数";
                foreach (string str in contents)
                {
                    data[index++] = str;
                }

                File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                AddLogInfo(string.Format("导出AssetBundle统计信息 '{0}' 成功.", exportFileName));
            }
            catch (Exception exception)
            {
                AddLogInfo(string.Format("导出AssetBundle统计信息 '{0}' 失败, 原因'{1}'.", exportFileName, exception));
            }
        }
    }
#endif
}