/*--------------------------------------------------------------
 * File: GameLayer.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/18 14:46:32 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public enum GameLayer 
{
    GameWorld_1 = 1, // 非UI的世界物体
    GameWorld_2,
    GameWorld_3,
    GameWorld_4,
    BOTTOM_1 = 10, // UI各类面板
    BOTTOM_2,
    BOTTOM_3,
    BOTTOM_4,

    MIDDLE_1, //5 弹窗等
    MIDDLE_2,
    MIDDLE_3,
    MIDDLE_4,

    TOP_1,  //9  引导等
    TOP_2,
    TOP_3,
    TOP_4,

    PEAK_1,  //17
    PEAK_2,
    PEAK_3,
    PEAK_4
}