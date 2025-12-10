/*--------------------------------------------------------------
 * File: MonoSingleton.cs
 * Author: Wang ShaoWen
 * Time: 2024/03/14 17:30:34 
 * Feedback: 614270423@qq.com
 * Copyright: Copyright © 2024 wangshaowen. All rights reserved.
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;  
    public static T Instance  
    {  
        get  
        {  
            if (_instance == null)  
            {  
                _instance = FindObjectOfType<T>();  
                if (_instance == null)  
                {  
                    GameObject singletonObject = new GameObject();  
                    _instance = singletonObject.AddComponent<T>();
                    singletonObject.name = typeof(T).Name;
                    DontDestroyOnLoad(singletonObject);  
                }  
            }  
            return _instance;  
        }  
    }
  
    protected virtual void Awake()  
    {  
        if (_instance == null)  
        {  
            _instance = this as T;  
            DontDestroyOnLoad(gameObject);  
        }  
        // 如果单例是直接挂载于场景中，切换场景又切换回来后，场景中会多一个，需要删除。
        // 不删除也不会出问题，_instance已被赋值，Instance拿到的始终会是第一个，只是稍微有点奇怪
        else  
        {  
            Destroy(gameObject);  
        }  
    }  
}