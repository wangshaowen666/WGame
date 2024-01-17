/*--------------------------------------------------------------
 * File: NewCSharpScript.cs
 * Author: Wang ShaoWen
 * Time: 2024/01/17 18:27:54
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 * Feedback: 614270423@qq.com
 *--------------------------------------------------------------
 */

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using Object = UnityEngine.Object;

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
        content = content.Replace("#Copyright#", "Copyright © 2024 wangshaowen. All rights reserved.");
        content = content.Replace("#Feedback#", "614270423@qq.com");
        
        // 将修改后的内容写入新创建的文件
        StreamWriter sw = new StreamWriter(Path.GetFullPath(pathName), false, new UTF8Encoding(true, false));
        sw.Write(content);
        sw.Close();
        
        // 导入资源
        AssetDatabase.ImportAsset(pathName);
        Object obj = AssetDatabase.LoadAssetAtPath(pathName, typeof(UnityEngine.Object));
        
        ProjectWindowUtil.ShowCreatedAsset(obj);
    }
}
