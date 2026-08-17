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
    /// 注册。成功返回 true，失败返回 false（看 resp.ErrorCode 里的原因）
    /// </summary>
    public async UniTask<NetMsg.RegisterResp> Register(string username, string password)
    {
        var req = new NetMsg.RegisterReq { Username = username, Password = password };
        var resp = await CoreMgr.Http.PostProto<NetMsg.RegisterReq, NetMsg.RegisterResp>(
            GameConfig.LoginServerUrl + "/register", req);
        if (resp.ErrorCode != NetMsg.ErrorCode.ErrorNone)
            Log.Error("注册失败, 错误码:", resp.ErrorCode);
        else
            Log.Info("注册成功");
        return resp;
    }

    /// <summary>
    /// 登录。成功后自动保存 token/playerId/username 到本地
    /// </summary>
    public async UniTask<NetMsg.LoginResp> Login(string username, string password)
    {
        var req = new NetMsg.LoginReq { Username = username, Password = password };
        var resp = await CoreMgr.Http.PostProto<NetMsg.LoginReq, NetMsg.LoginResp>(
            GameConfig.LoginServerUrl + "/login", req);

        if (resp.ErrorCode == NetMsg.ErrorCode.ErrorNone && !string.IsNullOrEmpty(resp.Token))
        {
            PlayerPrefsUtil.SetString(TokenKey, resp.Token);
            PlayerPrefsUtil.SetInt(PlayerIdKey, resp.PlayerId);
            PlayerPrefsUtil.SetString(UsernameKey, resp.Username);
            Log.Info("登录成功:", resp.Username);
        }
        else
        {
            Log.Error("登录失败, 错误码:", resp.ErrorCode);
        }
        return resp;
    }

    /// <summary>
    /// 用本地保存的 token 请求 /me，验证 token 是否仍有效
    /// </summary>
    public async UniTask<NetMsg.GetMeResp> GetMe()
    {
        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer " + Token }
        };
        return await CoreMgr.Http.GetProto<NetMsg.GetMeResp>(GameConfig.LoginServerUrl + "/me", headers);
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
