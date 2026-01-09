/*--------------------------------------------------------------
 * File: IUIPanel.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/09 17:02:21 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public interface IUIPanel 
{
    void OnInit();

    void OnOpen();

    void OnHide();

    void OnCover();

    void OnResume();

    void OnRecycle();
    
    void OnClose();
}