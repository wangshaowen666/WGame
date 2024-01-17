/*--------------------------------------------------------------
 * File: NewCSharpScript.cs
 * Author: Wang ShaoWen
 * Time: 2024/01/17 18:27:54
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 * Feedback: 614270423@qq.com
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PathUtil 
{
    /// <summary>
    /// 获取选中的文件夹路径
    /// </summary>
    /// <returns></returns>
    public static string GetSelectionAssetDirPath()
    {
        string path = "Assets";
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
            if (obj == null)
            {
                continue;
            }

            path = AssetDatabase.GetAssetPath(obj);
            if (Path.HasExtension(path))
            {
                path = Path.GetDirectoryName(path);
            }
            break;
        }

        return path;
    }
}
