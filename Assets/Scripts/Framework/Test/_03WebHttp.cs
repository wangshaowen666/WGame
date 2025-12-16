/*--------------------------------------------------------------
 * File: _03WebHttp.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/25 18:03:56 
 *--------------------------------------------------------------
 */

using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class _03WebHttp : MonoBehaviour
{
    private const string url = "http://localhost:80/MyServer";
    
    [ContextMenu("下载")]
    private void Test()
    {
        StartCoroutine(DownloadTest());
    }

    [ContextMenu("上传")]
    private void Test2()
    {
        StartCoroutine(UploadTest());
    }

    private IEnumerator DownloadTest()
    {
        string fileNm = "catalog_0.1.0.json";
        string filePath = Path.Combine(url, "Android/catalog/" + fileNm);
        string saveDirectory = Path.Combine(Application.persistentDataPath, "Downloads");
        string fullPath = Path.Combine(saveDirectory, fileNm);

        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        using (UnityWebRequest webRequest = UnityWebRequest.Get(filePath))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Log.Info("下载出错" + webRequest.error);
            }
            else
            {
                string content = webRequest.downloadHandler.text;
                Log.Info("文件内容:", content);
                
                byte[] bytes = webRequest.downloadHandler.data;
                File.WriteAllBytes(fullPath, bytes);
                Log.Info("文件已保存至：", fullPath);
            }
        }
    }

    private IEnumerator UploadTest()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "Downloads/上传测试.txt");

        if (!File.Exists(filePath))
        {
            Log.Error("文件不存在：", filePath);
            yield break;
        }

        string uploadUrl = Path.Combine(url, "simple_upload.php");
        byte[] bytes = File.ReadAllBytes(filePath);
        using (UnityWebRequest webRequest = new UnityWebRequest(uploadUrl, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bytes);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");
            
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Log.Info("上传失败", webRequest.error);
            }
            else
            {
                Log.Info("上传成功");
            }
        }
    }
    
    [Serializable]
    public class MyDataObject
    {
        public string name;
        public int score;
    }

    private void Start()
    {
        // 域名:端口/路径 ？是查询字符串起始符 后面跟参数键值对 多个参数用&连接
        //StartCoroutine(SendGetRequest("http://localhost:6666/api/data?id=1002"));
        // 改成与tm相同的格式
        StartCoroutine(SendGetRequest("http://localhost:6666/?CH=Api&Opt=CheckUserCountry&id=999"));
            
        
        MyDataObject data = new MyDataObject{ name = "Wsw", score = 100 };
        string json = JsonUtility.ToJson(data);
        StartCoroutine(SendPostRequest("http://localhost:6666/api/?CH=Api&Opt=CheckUserCountry", json));
    }

    IEnumerator SendGetRequest(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Log.Info("Get请求失败" + webRequest.error);
            }
            else
            {
                string content = webRequest.downloadHandler.text;
                Log.Info("Get获取内容为:", content);
            }
        }
    }

    IEnumerator SendPostRequest(string url, string jsonData)
    {
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            // 重要：设置请求头，告知服务器发送的是JSON数据
            webRequest.SetRequestHeader("Content-Type", "application/json");
            
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Log.Info("Post请求失败" + webRequest.error);
            }
            else
            {
                string content = webRequest.downloadHandler.text;
                Log.Info("Post获取内容为:", content);
            }
        }
        
    }
}