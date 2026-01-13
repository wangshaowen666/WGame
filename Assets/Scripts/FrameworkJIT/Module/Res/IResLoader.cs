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
    
    // 这里如果不提供userData参数，当callback执行时 想使用原调用方法中的局部参数 则只能使用匿名函数的形式
    // 匿名函数的闭包实现生成了匿名类存储局部变量，不如自己显示创建类通过池管理
    void LoadAsync<T>(string key, LoadAssetCallback<T> callback, object userData = null);
}