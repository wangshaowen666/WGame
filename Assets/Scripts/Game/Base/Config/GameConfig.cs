/*--------------------------------------------------------------
 * File: GameConfig.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/10 18:55:55 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameConfig
{
    // 屏幕标准尺寸
    public static Vector2Int ScreenSize = new Vector2Int(1080, 1920);

    public const string MapCamera = "MapCamera";
    public const string BattleCamera = "BattleCamera";
    public const string UICamera = "UICamera";
    
    // 启动游戏获取服务器设置网址
    public const string ServerSettingURL = "http://localhost:6666/?CH=Api&Opt=GetServerSetting";
}

