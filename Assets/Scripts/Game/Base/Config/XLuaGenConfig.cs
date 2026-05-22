/*--------------------------------------------------------------
 * File: XLuaGenConfig.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/29 18:01:27 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

public static class XLuaGenConfig
{
    // C#静态调用Lua的配置（包括事件的原型），仅可以配delegate，interface
    [CSharpCallLua] 
    public static List<Type> CSharpCallLua = new List<Type>()
    {

    };
    
    //lua中要使用到C#库的配置，比如C#标准库，或者Unity API，第三方库等。
    [LuaCallCSharp] 
    public static List<Type> LuaCallCSharp = new List<Type>()
    {
        //typeof(ResMgr),
    };
    
    //黑名单
    [BlackList] public static List<List<string>> BlackList = new List<List<string>>()
    {
        new List<string>() { "UnityEngine.Light", "shadowRadius" },
        new List<string>() { "UnityEngine.Light", "SetLightDirty" },
        new List<string>() { "UnityEngine.Light", "shadowAngle" },
        new List<string>() { "UnityEngine.Light", "shadowAngle" },
    };

    // [Hotfix] public static List<Type> by_field = new List<Type>()
    // {
    //     typeof(TMP_Settings)
    // };
}