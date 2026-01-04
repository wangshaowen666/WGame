/*--------------------------------------------------------------
 * File: AOTConfig.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/04 18:18:27 
 *--------------------------------------------------------------
 */

using UnityEngine;

public static class AOTConfig
{
    // 单机模式
    public const bool Standalone = false;
    
    // 登陆界面路径
    public const string LoginPath = "LoginPanel/LoginRoot";
    
    // 屏幕标准尺寸
    public static Vector2Int ScreenSize = new Vector2Int(1080, 1920);
    
    // 启动游戏获取服务器设置网址
    public const string ServerSettingURL = "http://localhost:6666/?CH=Api&Opt=GetServerSetting";
}