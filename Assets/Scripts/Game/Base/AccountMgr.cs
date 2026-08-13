/*--------------------------------------------------------------
 * File: AccountMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/13 14:00:00 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 账号服务：注册、登录、token 校验。登录态存 PlayerPrefs 持久化
/// </summary>
public class AccountMgr : ManagerBase
{
    private const string TokenKey = "login_token";
    private const string PlayerIdKey = "login_playerId";
    private const string UsernameKey = "login_username";

    public string Token => PlayerPrefsUtil.GetString(TokenKey);
    public int PlayerId => PlayerPrefsUtil.GetInt(PlayerIdKey);
    public string Username => PlayerPrefsUtil.GetString(UsernameKey);
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    /// <summary>
    /// 注册。成功返回 true，失败返回 false（可看 result.Data 里的 error）
    /// </summary>
    public async UniTask<HttpResult> Register(string username, string password)
    {
        // 注意：ParseUtil.ToJson 用的是 MiniJSON，只序列化字段不序列化属性，
        // 匿名类型会变成空对象 {}，所以用 Dictionary 传参
        var body = new Dictionary<string, object>
        {
            { "username", username },
            { "password", password },
        };
        return await CoreMgr.Http.Post(GameConfig.LoginServerUrl + "/register", body);
    }

    /// <summary>
    /// 登录。成功后自动保存 token/playerId/username 到本地
    /// </summary>
    public async UniTask<HttpResult> Login(string username, string password)
    {
        var body = new Dictionary<string, object>
        {
            { "username", username },
            { "password", password },
        };
        var result = await CoreMgr.Http.Post(GameConfig.LoginServerUrl + "/login", body);

        if (result.IsSuccess && result.Data != null)
        {
            PlayerPrefsUtil.SetString(TokenKey, result.Data["token"]?.ToString());
            PlayerPrefsUtil.SetInt(PlayerIdKey, Convert.ToInt32(result.Data["playerId"]));
            PlayerPrefsUtil.SetString(UsernameKey, result.Data["username"]?.ToString());
            Log.Info("登录成功:", Username);
        }
        return result;
    }

    /// <summary>
    /// 用本地保存的 token 请求 /me，验证 token 是否仍有效
    /// </summary>
    public async UniTask<HttpResult> GetMe()
    {
        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer " + Token }
        };
        return await CoreMgr.Http.Get(GameConfig.LoginServerUrl + "/me", headers);
    }

    /// <summary>
    /// 退出登录，清除本地登录态
    /// </summary>
    public void Logout()
    {
        PlayerPrefsUtil.DeleteKey(TokenKey);
        PlayerPrefsUtil.DeleteKey(PlayerIdKey);
        PlayerPrefsUtil.DeleteKey(UsernameKey);
    }
}
