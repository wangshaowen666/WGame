/*--------------------------------------------------------------
 * File: JoystickInput.cs
 * Author: Wsw
 * Time: 2026/08/31
 *--------------------------------------------------------------
 */

using UnityEngine;

/// <summary>
/// 虚拟摇杆输入（纯手机项目移动入口，P1）：
/// 包装 GameJoystick，把摇杆输出的方向向量（float，|dir|<=1，已含基类死区处理）转为 Fix 定点。
/// 输入采样是系统边界：物理摇杆模拟量在帧边界转定点后进入逻辑层，不参与确定性演算内部，
/// 因此不破坏确定性（同一输入序列 + 同一种子 => 同结果）。
/// 阶段 5 联机：网络帧输入单独实现 IBattleInput，本类仅单机摇杆用。
/// </summary>
public class JoystickInput : IBattleInput
{
    private readonly GameJoystick _joystick;

    public int PlayerIndex { get; }

    public JoystickInput(GameJoystick joystick, int playerIndex = 0)
    {
        _joystick = joystick;
        PlayerIndex = playerIndex;
    }

    public void Sample(out Fix dirX, out Fix dirY)
    {
        var dir = _joystick.Direction;
        dirX = Fix.FromDouble(dir.x);
        dirY = Fix.FromDouble(dir.y);
    }
}
