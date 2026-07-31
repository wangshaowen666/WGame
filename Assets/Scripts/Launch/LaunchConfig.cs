/*--------------------------------------------------------------
 * File: AOTConfig.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/04 18:18:27 
 *--------------------------------------------------------------
 */

using UnityEngine;

public static class LaunchConfig
{
    // 游戏ui根节点名
    public const string UIRoot = "UIRoot";
    
    // ui相机名
    public const string UICamera = "UICamera";
    
    // 登陆界面路径
    public const string LoginPath = "LoginPanel/LoginPanel";
    
    // 屏幕标准尺寸
    public static Vector2Int ScreenSize = new Vector2Int(1080, 1920);
    
    // 启动游戏获取服务器设置网址
    public const string ServerSettingURL = "http://localhost:6666/?CH=Api&Opt=GetServerSetting";
    
    // 跳过版本检查
    public const bool SkipVersionCheck = true;
}