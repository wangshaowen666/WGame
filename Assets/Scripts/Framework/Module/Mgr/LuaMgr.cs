using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

public class LuaMgr : ManagerBase
{
    private LuaEnv _luaEnv; 

    public void InitLuaEnv()
    {
        _luaEnv = new LuaEnv();
        _luaEnv.AddLoader(CustomLoader);
        _luaEnv.DoString("require 'Main'");
    }
    
    private byte[] CustomLoader(ref string filepath)
    {
        var file = CoreMgr.Res.LoadSync<TextAsset>(filepath + ".lua");
        if (file == null)
        {
            Debug.LogError("未找到Lua文件: " + filepath);
            return null;
        }
        else
        {
            return file.bytes;
        }
    }
}
