/*--------------------------------------------------------------
 * File: IBattleInput.cs
 * Author: Wsw
 * Time: 2026/08/31
 *--------------------------------------------------------------
 */

/// <summary>
/// 战斗输入抽象：输出归一化定点方向向量（逻辑层输入源，见任务清单"总体架构决策"4）。
/// - 实现：虚拟摇杆（P1 GameJoystick，JoystickInput）；阶段 5 联机时网络帧输入再实现一版
/// - 输入只在帧边界采样进 Tick（由 LocalDriver 在帧消费前遍历所有已注册输入源调用 Sample），
///   逻辑层禁止读 Input.*；输入是系统边界的外部事件，不参与确定性演算
/// </summary>
public interface IBattleInput
{
    /// <summary>玩家索引（0=P1，1=P2；阶段 5 联机双人按序供给）</summary>
    int PlayerIndex { get; }

    /// <summary>帧边界采样：输出归一化定点方向向量（0,0 = 无输入）</summary>
    void Sample(out Fix dirX, out Fix dirY);
}
