/*--------------------------------------------------------------
 * File: IResLoader.cs
 * Author: Wang ShaoWen
 * Time: 2025/09/01 10:56:06 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public interface IResLoader
{
    T LoadSync<T>(string key);
    void LoadAsync<T>(string key, Action<T> callback);
}