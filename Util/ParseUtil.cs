/*--------------------------------------------------------------
 * File: EncodeUtil.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/26 15:35:19 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using MiniJSON;

/*
 * MD5算法目前被认为是不安全的，尤其是在需要防范碰撞攻击（即两个不同的输入产生相同的哈希值）的密码学场景中
 *  
 */

public static class ParseUtil 
{
    
    public static string MD5Str(string source)
    {
        MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
        byte[] dataRaw = System.Text.Encoding.UTF8.GetBytes(source);
        byte[] data = md5.ComputeHash(dataRaw, 0, dataRaw.Length);
        md5.Clear();

        string destString = "";
        for (int i = 0; i < data.Length; i++)
        {
            destString += System.Convert.ToString(data[i], 16).PadLeft(2, '0');
        }

        destString = destString.PadLeft(32, '0');
        return destString;
    }


    
    
    public static string ToJson(this object obj)
    {
        return Json.Serialize(obj);
    }

    /// <summary>
    /// Json转换成obj，复杂结构只支持Dictionary(string,object)和List(object),需自行转化
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static object ToObj(this string json)
    {
        try
        {
            return Json.Deserialize(json);
        }
        catch (Exception e)
        {
            Log.Error("解析出错：", json);
            return null;
        }
    }
}