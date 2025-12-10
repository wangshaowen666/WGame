/*--------------------------------------------------------------
 * File: _09MiniJson.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/08 15:43:37 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using System.Linq;
using MiniJSON;
using UnityEngine;

public class _09ParseTest : MonoBehaviour
{
    void Start()
    {
        _09JsonTest();
        _09TestBinary();
    }

    private void _09TestBinary()
    {
        List<string> m = new List<string>{"sfs", "kjk", "435"};

        Timer.StartDelay(0, _i =>
        {
            int count = 100000;
            TimeProfiler.RecordTimeStart("二进制转化1");
            for (int i = 0; i < count; i++)
            {
                var b = ParseUtil.ToBinary(m);
                var n = ParseUtil.DeBinary<List<string>>(b);
                //Log.Info("反序列化二进制", ParseUtil.ToJson(n));
            }
            TimeProfiler.RecordTimeStop("二进制转化1");
        
            TimeProfiler.RecordTimeStart("二进制转化2");
            for (int i = 0; i < count; i++)
            {
                // var b2 = ParseUtil.BinarySerialize(m);
                // var n2 = ParseUtil.BinaryDeserialize<List<string>>(b2);
                //Log.Info("反序列化二进制2", ParseUtil.ToJson(n2));
            }
            TimeProfiler.RecordTimeStop("二进制转化2");
        
            TimeProfiler.DebugRecordTime();
        }, false);
    }
    
    private void _09JsonTest()
    {
        List<string> m = new List<string>{"sfs", "kjk", "435"};
        List<string> m2 = new List<string>{"333", "4444", "111", "sdfsfhsjkhfkjshfkjshfkj"};

        Dictionary<string, List<string>> dic = new Dictionary<string, List<string>>();

        Dictionary<int, Dictionary<string, List<string>>> dic2 = new Dictionary<int, Dictionary<string, List<string>>>();
       
        dic.Add("d1", m);
        dic.Add("d2", m2);
        dic2.Add(0, dic);

        List<float> f = new List<float> { 2.0f, 8.5f, 3.3f, 112.5f };

        int[] i = new[] { 10, 8, 7, 40, 9 };
       
        Log.Info("字典打印：", ParseUtil.ToJson(dic));
        Log.Info("列表打印", ParseUtil.ToJson(f));
        Log.Info("数组打印", ParseUtil.ToJson(i));
        Log.Info("字典打印2：", ParseUtil.ToJson(dic2));

        var mm = Json.Serialize(dic);
        Dictionary<string, object> d = ParseUtil.DeJson(mm) as Dictionary<string, object>;
        var d2 = d.ToDictionary(k => k.Key, v => v.Value == null? "" : v.Value.ToString());
        Log.Info("反序列化后结果", ParseUtil.ToJson(d2));

        Dictionary<string, List<string>> d3 = new Dictionary<string, List<string>>();
        foreach (var v in d)
        {
            if (v.Value is List<object> list)
            {
                List<string> l = new List<string>();
                for (int j = 0; j < list.Count; j++)
                {
                    l.Add(list[j].ToString());
                }
               
                d3[v.Key] = l;
            }
            else
            {
                Log.Error("格式不对");
            }
        }
       
        Log.Info("反序列化后结果2", ParseUtil.ToJson(d3));
    }
}