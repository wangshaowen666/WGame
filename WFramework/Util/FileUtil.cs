/*--------------------------------------------------------------
 * File: FileUtil.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/11 14:21:06 
 *--------------------------------------------------------------
 */

using System;
using System.IO;
using UnityEngine;

public static class FileUtil
{
    // 输出路径
    private const string OUT_PATH = "/Users/wangshaowen/Desktop";
    
    public static void WriteText(string content, string fileName)
    {
        string path = Path.Combine(OUT_PATH, fileName);
        File.WriteAllText(path, content);
    }
}