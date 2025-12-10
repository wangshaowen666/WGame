/*--------------------------------------------------------------
 * File: IDataProvider.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/13 16:02:43 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public interface IDataProvider
{
    event EventHandler<ReadDataSuccessEventArgs> ReadDataSuccess;
    event EventHandler<ReadDataUpdateEventArgs> ReadDataUpdate;
    event Action<string, float, object> ReadDataFail;
    event Action<string, string, int, int, object> ReadDataDependencyAsset;

    /// <summary>
    /// 读取数据。
    /// </summary>
    /// <param name="dataAssetName">内容资源名称。</param>
    void ReadData(string dataAssetName);

    /// <summary>
    /// 读取数据。
    /// </summary>
    /// <param name="dataAssetName">内容资源名称。</param>
    /// <param name="priority">加载数据资源的优先级。</param>
    void ReadData(string dataAssetName, int priority);

    /// <summary>
    /// 读取数据。
    /// </summary>
    /// <param name="dataAssetName">内容资源名称。</param>
    /// <param name="userData">用户自定义数据。</param>
    void ReadData(string dataAssetName, object userData);

    /// <summary>
    /// 读取数据。
    /// </summary>
    /// <param name="dataAssetName">内容资源名称。</param>
    /// <param name="priority">加载数据资源的优先级。</param>
    /// <param name="userData">用户自定义数据。</param>
    void ReadData(string dataAssetName, int priority, object userData);

    /// <summary>
    /// 解析内容。
    /// </summary>
    /// <param name="dataString">要解析的内容字符串。</param>
    /// <returns>是否解析内容成功。</returns>
    bool ParseData(string dataString);

    /// <summary>
    /// 解析内容。
    /// </summary>
    /// <param name="dataString">要解析的内容字符串。</param>
    /// <param name="userData">用户自定义数据。</param>
    /// <returns>是否解析内容成功。</returns>
    bool ParseData(string dataString, object userData);

    /// <summary>
    /// 解析内容。
    /// </summary>
    /// <param name="dataBytes">要解析的内容二进制流。</param>
    /// <returns>是否解析内容成功。</returns>
    bool ParseData(byte[] dataBytes);

    /// <summary>
    /// 解析内容。
    /// </summary>
    /// <param name="dataBytes">要解析的内容二进制流。</param>
    /// <param name="userData">用户自定义数据。</param>
    /// <returns>是否解析内容成功。</returns>
    bool ParseData(byte[] dataBytes, object userData);

    /// <summary>
    /// 解析内容。
    /// </summary>
    /// <param name="dataBytes">要解析的内容二进制流。</param>
    /// <param name="startIndex">内容二进制流的起始位置。</param>
    /// <param name="length">内容二进制流的长度。</param>
    /// <returns>是否解析内容成功。</returns>
    bool ParseData(byte[] dataBytes, int startIndex, int length);

    /// <summary>
    /// 解析内容。
    /// </summary>
    /// <param name="dataBytes">要解析的内容二进制流。</param>
    /// <param name="startIndex">内容二进制流的起始位置。</param>
    /// <param name="length">内容二进制流的长度。</param>
    /// <param name="userData">用户自定义数据。</param>
    /// <returns>是否解析内容成功。</returns>
    bool ParseData(byte[] dataBytes, int startIndex, int length, object userData);
}