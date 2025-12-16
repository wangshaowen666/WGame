/*--------------------------------------------------------------
 * File: Config.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/10 18:50:04 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using UnityEngine;

public static class Config 
{
#if UNITY_EDITOR
    // 自定义C#脚本文件路径
    public const string ScriptTemplatePath = "Assets/Scripts/Framework/Base/ScriptTemplates/CustomC#Script.txt";
    // 自定义Lua脚本文件路径
    public const string LuaTemplatePath = "Assets/Scripts/Framework/Base/ScriptTemplates/CustomLuaScript.txt";
#endif
}