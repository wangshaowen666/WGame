/*--------------------------------------------------------------
 * File: NewCSharpScript.cs
 * Author: Wang ShaoWen
 * Time: 2024/01/17 18:27:54
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using Object = UnityEngine.Object;

public class ScriptCreator 
{
    private static readonly string ScriptTemplatePath = "Assets/WFramework/CustomScript/ScriptTemplates/CustomC#Script.txt";
    
    [MenuItem("Assets/Create/Custom C# Script", false, 70)]
    private static void CreateCustomCSharpScript()
    {
        Create(ScriptTemplatePath, "NewCSharpScript.cs");
    }

    private static void Create(string path, string defaultName)
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("警告", "无法在游戏运行时或代码编译时创建脚本", "确定");
            return;
        }

        string folderPath = GetSelectionAssetDirPath();
        Texture2D icon = EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D;
       
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
            ScriptableObject.CreateInstance<CreateCustomScriptAction>(),
            folderPath + "/" + defaultName, icon,
            path);
    }
    
    /// <summary>
    /// 获取选中文件的文件夹路径
    /// </summary>
    /// <returns></returns>
    private static string GetSelectionAssetDirPath()
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

public class CreateCustomScriptAction : EndNameEditAction
{
    public override void Action(int instanceId, string pathName, string resourceFile)
    {
        // 读入模板内容
        StreamReader sr = new StreamReader(resourceFile);
        string content = sr.ReadToEnd();
        sr.Close();

        // 修改内容中的部分内容
        content = content.Replace("#SCRIPTNAME#", Path.GetFileNameWithoutExtension(pathName));
        content = content.Replace("#Author#", "Wang ShaoWen");
        content = content.Replace("#TIME#", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
        //content = content.Replace("#Copyright#", "Copyright © 2024 wangshaowen. All rights reserved.");
        //content = content.Replace("#Feedback#", "614270423@qq.com");
        
        // 将修改后的内容写入新创建的文件
        StreamWriter sw = new StreamWriter(Path.GetFullPath(pathName), false, new UTF8Encoding(false, false));
        sw.Write(content);
        sw.Close();
        
        // 导入资源
        AssetDatabase.ImportAsset(pathName);
        Object obj = AssetDatabase.LoadAssetAtPath(pathName, typeof(UnityEngine.Object));
        
        ProjectWindowUtil.ShowCreatedAsset(obj);
    }
}
