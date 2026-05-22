/*--------------------------------------------------------------
 * File: ScrollableBackground.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/12/31 14:16:52 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class ScrollableBackground : MonoBehaviour
{
    [SerializeField]
    private float _scrollSpeed = -0.25f;
    [SerializeField]
    private float _tileSize = 30f;

    private Transform _cacheTrans;
    private Vector3 _startPos;
    private float _high;

    private void Start()
    {
        _cacheTrans = transform;
        _startPos = _cacheTrans.position;
    }

    void Update()
    {
        _high = Mathf.Repeat(Time.time * _scrollSpeed, _tileSize);
        _cacheTrans.position = _startPos + Vector3.forward * _high;
    }
}