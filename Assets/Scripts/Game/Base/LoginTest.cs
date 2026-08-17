/*--------------------------------------------------------------
 * File: LoginTest.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/13 14:30:00 
 *--------------------------------------------------------------
 */

using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 登录流程测试脚本，挂到场景任意物体上，右键 Inspector 调用
/// </summary>
public class LoginTest : MonoBehaviour
{
    [Header("测试账号")]
    public string username = "alice";
    public string password = "123456";

    [ContextMenu("测试注册")]
    private async void TestRegister()
    {
        var result = await GameMgr.Account.Register(username, password);
        if (result.ErrorCode == 0)
            Log.Info("注册成功");
        else
            Log.Error("注册失败:", result.ErrorCode);
    }

    [ContextMenu("测试登录")]
    private async void TestLogin()
    {
        var result = await GameMgr.Account.Login(username, password);
        if (result.ErrorCode == 0)
            Log.Info("登录成功, token 已保存");
        else
            Log.Error("登录失败:", result.ErrorCode);
    }

    [ContextMenu("测试 /me 校验 token")]
    private async void TestGetMe()
    {
        if (!GameMgr.Account.IsLoggedIn)
        {
            Log.Error("未登录，请先测试登录");
            return;
        }

        var result = await GameMgr.Account.GetMe();
        if (result.ErrorCode == 0)
            Log.Info("/me 校验通过:", result.Username);
        else
            Log.Error("/me 校验失败:", result.ErrorCode);
    }

    [ContextMenu("测试退出登录")]
    private void TestLogout()
    {
        GameMgr.Account.Logout();
        Log.Info("已退出登录");
    }

    [ContextMenu("测试加载养成数据")]
    private async void TestLoadData()
    {
        if (!GameMgr.Account.IsLoggedIn)
        {
            Log.Error("未登录，请先测试登录");
            return;
        }

        var result = await GameMgr.PlayerData.Load();
        if (result.ErrorCode == 0)
            Log.Info("养成数据: 金币", GameMgr.PlayerData.Gold, "关卡", GameMgr.PlayerData.StageProgress);
        else
            Log.Error("加载失败:", result.ErrorCode);
    }

    [ContextMenu("测试保存养成数据")]
    private async void TestSaveData()
    {
        if (!GameMgr.Account.IsLoggedIn)
        {
            Log.Error("未登录，请先测试登录");
            return;
        }

        GameMgr.PlayerData.AddGold(100);
        var result = await GameMgr.PlayerData.Save();
        if (result.ErrorCode == 0)
            Log.Info("保存成功");
        else
            Log.Error("保存失败:", result.ErrorCode);
    }
}
