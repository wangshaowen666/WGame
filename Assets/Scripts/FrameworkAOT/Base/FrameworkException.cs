/*--------------------------------------------------------------
 * File: FrameworkException.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/11 16:21:35 
 *--------------------------------------------------------------
 */

using System;

public class GameException : Exception
{
    public GameException() { }
    public GameException(string message) : base(message) { }
    public GameException(string message, Exception innerException) : base(message, innerException) { } 
}