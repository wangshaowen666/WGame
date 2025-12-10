/*--------------------------------------------------------------
 * File: Print.cs
 * Author: Wang ShaoWen
 * Time: 2024/01/18 11:27:03 
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class Print : MonoBehaviour
{
    private void Start()
    {
        Debug.Log($"[Print] GameObject:{name}");
    }
}