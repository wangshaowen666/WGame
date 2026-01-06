/*--------------------------------------------------------------
 * File: LoginPanel.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/22 11:23:21 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;
using TMPro;

public class LoginPanel : MonoBehaviour
{
    public TextMeshProUGUI tipText;
    public SlicedFilledImage progressImg;

    public void SetTip(string tip, float progress)
    {
        tipText.text = tip;
        progressImg.fillAmount = progress;
    }
}