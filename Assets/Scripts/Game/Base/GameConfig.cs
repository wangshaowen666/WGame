/*--------------------------------------------------------------
 * File: GameConfig.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/07 13:28:00 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public static class GameConfig 
{
    // 游戏内场景
    public const string MainScene = "Main";
    public const string BattleScene = "Battle";
    public const string NetworkScene = "Network";

    // 登录服务器地址（本机调试用，真机需改为局域网/公网 IP）
    public const string LoginServerUrl = "http://localhost:5049";
}