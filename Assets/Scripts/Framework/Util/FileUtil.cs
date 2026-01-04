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
    
    public static void WriteFile(string content, string fileName)
    {
        string path = Path.Combine(OUT_PATH, fileName);
        File.WriteAllText(path, content);
    }

    public static string ReadFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
                return content;
            }

            Log.Error("未找到文件：", path);
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public static void CopyDirectory(string sourceDir, string destinationDir, bool overwrite = true)
    {
        if (!Directory.Exists(sourceDir))
        {
            Log.Error($"源目录不存在: {sourceDir}");
            return;
        }

        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        string[] files = Directory.GetFiles(sourceDir);
        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destinationDir, fileName);
            File.Copy(file, destFile, overwrite); 
        }

        string[] subDirs = Directory.GetDirectories(sourceDir);
        foreach (string subDir in subDirs)
        {
            string dirName = Path.GetFileName(subDir);
            string destSubDir = Path.Combine(destinationDir, dirName);
            CopyDirectory(subDir, destSubDir); 
        }
    }

    public static string GetFileLength(long byteLength)
    {
        if (byteLength < 1024L)
        {
            return $"{byteLength} B";
        }
        
        if (byteLength < 1048576L)
        {
            return $"{byteLength / 1024f:F2} KB";
        }
        
        if (byteLength < 1073741824L) // 2 ^ 30
        {
            return $"{byteLength / 1048576f:F2} MB";
        }
        
        return $"{byteLength / 1073741824f:F2} GB";
    }
}