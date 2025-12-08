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

public class _09MiniJson : MonoBehaviour
{
    void Start()
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
       
       
        Log.Info("字典打印：", dic.ToJson());
        Log.Info("列表打印", f.ToJson());
        Log.Info("数组打印", i.ToJson());
        Log.Info("字典打印2：", dic2.ToJson());
       
       
        Log.Info("字典打印：", Json.Serialize(dic));
        Log.Info("列表打印", Json.Serialize(f));
        Log.Info("数组打印", Json.Serialize(i));
        Log.Info("字典打印2：",Json.Serialize(dic2));


        var mm = Json.Serialize(dic);
        Dictionary<string, object> d = mm.ToObj() as Dictionary<string, object>;
        var d2 = d.ToDictionary(k => k.Key, v => v.Value == null? "" : v.Value.ToString());
        Log.Info("反序列化后结果", d2.ToJson());

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
       
        Log.Info("反序列化后结果2", d3.ToJson());
    }
}