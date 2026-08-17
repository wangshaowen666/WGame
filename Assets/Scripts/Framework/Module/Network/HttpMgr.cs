/*--------------------------------------------------------------
 * File: HttpMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/26 15:31:12 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// HTTP 请求结果
/// </summary>
public class HttpResult
{
    /// <summary>HTTP 状态码：200 成功 / 400 请求错误 / 401 未授权等</summary>
    public long StatusCode;
    /// <summary>响应原始 JSON 字符串</summary>
    public string RawData;
    /// <summary>解析后的字典（复杂结构需自行转换）</summary>
    public Dictionary<string, object> Data;

    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
}

public class HttpMgr : ManagerBase
{
    /// <summary>
    /// POST 请求
    /// </summary>
    /// <param name="url">接口地址</param>
    /// <param name="args">请求体对象（支持匿名类型）</param>
    /// <param name="headers">自定义请求头，如 Authorization</param>
    public async UniTask<HttpResult> Post(string url, object args = null, Dictionary<string, string> headers = null)
    {
        using var webRequest = new UnityWebRequest(url, "POST");

        if (args != null)
        {
            var json = ParseUtil.ToJson(args);
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        }
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");

        ApplyHeaders(webRequest, headers);

        await webRequest.SendWebRequest();
        return ParseResult(webRequest);
    }

    /// <summary>
    /// GET 请求
    /// </summary>
    /// <param name="url">接口地址</param>
    /// <param name="headers">自定义请求头，如 Authorization</param>
    public async UniTask<HttpResult> Get(string url, Dictionary<string, string> headers = null)
    {
        using var webRequest = UnityWebRequest.Get(url);
        webRequest.downloadHandler = new DownloadHandlerBuffer();

        ApplyHeaders(webRequest, headers);

        await webRequest.SendWebRequest();
        return ParseResult(webRequest);
    }

    /// <summary>
    /// POST 请求（proto 消息体，Content-Type: application/x-protobuf）
    /// </summary>
    /// <typeparam name="TReq">请求 proto 类型</typeparam>
    /// <typeparam name="TResp">响应 proto 类型</typeparam>
    public async UniTask<TResp> PostProto<TReq, TResp>(string url, TReq req, Dictionary<string, string> headers = null)
        where TReq : class, IMessage<TReq>, new()
        where TResp : class, IMessage<TResp>, new()
    {
        using var webRequest = new UnityWebRequest(url, "POST");

        if (req != null)
            webRequest.uploadHandler = new UploadHandlerRaw(req.ToByteArray());
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/x-protobuf");

        ApplyHeaders(webRequest, headers);

        await webRequest.SendWebRequest();
        return ParseProtoResp<TResp>(webRequest);
    }

    /// <summary>
    /// GET 请求（proto 响应体）
    /// </summary>
    public async UniTask<TResp> GetProto<TResp>(string url, Dictionary<string, string> headers = null)
        where TResp : class, IMessage<TResp>, new()
    {
        using var webRequest = UnityWebRequest.Get(url);
        webRequest.downloadHandler = new DownloadHandlerBuffer();

        ApplyHeaders(webRequest, headers);

        await webRequest.SendWebRequest();
        return ParseProtoResp<TResp>(webRequest);
    }

    private static TResp ParseProtoResp<TResp>(UnityWebRequest webRequest) where TResp : class, IMessage<TResp>, new()
    {
        // 无论 HTTP 状态码，都尝试解析响应体（错误时 proto 里有 error 字段）
        var bytes = webRequest.downloadHandler?.data;
        if (bytes != null && bytes.Length > 0)
        {
            try
            {
                return new MessageParser<TResp>(() => new TResp()).ParseFrom(bytes);
            }
            catch (Exception e)
            {
                Log.Error("解析 proto 响应失败:", e.Message, "状态码:", webRequest.responseCode);
            }
        }
        return new TResp();
    }

    private static void ApplyHeaders(UnityWebRequest request, Dictionary<string, string> headers)
    {
        if (headers == null) return;
        foreach (var kv in headers)
            request.SetRequestHeader(kv.Key, kv.Value);
    }

    private static HttpResult ParseResult(UnityWebRequest webRequest)
    {
        var result = new HttpResult { StatusCode = webRequest.responseCode };

        // 连接错误 / 数据处理错误（HTTP 4xx/5xx 会进 ProtocolError，但响应体仍可读）
        if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
            webRequest.result == UnityWebRequest.Result.DataProcessingError)
        {
            Log.Error("HTTP 请求失败:", webRequest.error);
            return result;
        }

        result.RawData = webRequest.downloadHandler.text;
        if (!string.IsNullOrEmpty(result.RawData))
            result.Data = ParseUtil.DeJson(result.RawData) as Dictionary<string, object>;

        return result;
    }
}
