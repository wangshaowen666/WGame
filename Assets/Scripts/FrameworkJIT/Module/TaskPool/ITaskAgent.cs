/*--------------------------------------------------------------
 * File: ITaskAgent.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/01 14:40:11 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public interface ITaskAgent<T> where T : TaskBase 
{
    T Task { get; }

    void Init();
    
    void Update(float deltaTime, float realDeltaTime);
}