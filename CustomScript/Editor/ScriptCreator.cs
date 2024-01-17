/*--------------------------------------------------------------
 * File: NewCSharpScript.cs
 * Author: Wang ShaoWen
 * Time: 2024/01/17 18:27:54
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 * Feedback: 614270423@qq.com
 *--------------------------------------------------------------
 */

using UnityEditor;
using UnityEngine;

public class ScriptCreator 
{
    private static readonly string ScriptTemplatePath = "Assets/WFramework/CustomScript/ScriptTemplates/CustomC#Script.cs.txt";
    
    [MenuItem("Assets/Create/Custom C# Script", false, 70)]
    private static void CreateCustomCSharpScript()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("警告", "无法在游戏运行时或代码编译时创建脚本", "确定");
            return;
        }

        Texture2D icon = EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D;
        string scriptDirPath = PathUtil.GetSelectionAssetDirPath();
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
            ScriptableObject.CreateInstance<CreateCustomScriptAction>(),
            scriptDirPath + "/NewCSharpScript.cs", icon,
            ScriptTemplatePath);
    }
}
