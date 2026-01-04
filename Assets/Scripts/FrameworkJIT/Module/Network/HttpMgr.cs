/*--------------------------------------------------------------
 * File: HttpMgr.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/26 15:31:12 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class HttpMgr : Singleton<HttpMgr>
{
    // todo 通信要有proto，后续添加
    // todo 数据处理部分放在其他线程，SendWebRequest放在主线程

    private HttpMgr()
    {
        
    }

    public async UniTask<Dictionary<string, object>> Post(string url, object args = null)
    {
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            if (args != null)
            {
                var jsonArgs = JsonUtility.ToJson(args);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonArgs);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            // 重要：设置请求头，告知服务器发送的是JSON数据
            webRequest.SetRequestHeader("Content-Type", "application/json");
            await webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Log.Info("Post请求失败" + webRequest.error);
                return null;
            }
            else
            {
                // todo 返回的json只支持到Dictionary<string,object>
                string content = webRequest.downloadHandler.text;
                await UniTask.SwitchToThreadPool();
                Dictionary<string, object> ret = ParseUtil.DeJson(content) as Dictionary<string, object>;
                await UniTask.SwitchToMainThread();
                return ret;
            }
        }
    }
}