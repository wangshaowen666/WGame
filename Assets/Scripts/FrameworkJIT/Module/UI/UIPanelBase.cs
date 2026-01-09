/*--------------------------------------------------------------
 * File: UIPanelBase.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/31 18:48:58 
 *--------------------------------------------------------------
 */

using UnityEngine;
using DG.Tweening;

public enum PanelState
{
    None,
    Active,
    Hide,
    Cover,
    Recycle,
}

[RequireComponent(typeof(CanvasGroup))]
public class UIPanelBase : MonoBehaviour, IUIPanel
{
    private const float FadeTime = 0.3f;
    private CanvasGroup _canvasGroup;

    public PanelState State { get; private set; }

    public virtual void OnInit()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public virtual void OnOpen()
    {
        _canvasGroup.alpha = 0;
        _canvasGroup.DOFade(1, FadeTime);
        
        State = PanelState.Active;
    }

    public virtual void OnHide()
    {
        _canvasGroup.alpha = 0;
        State = PanelState.Hide;
    }

    public virtual void OnCover()
    {
        State = PanelState.Cover;
    }
    
    public virtual void OnResume()
    {
        switch (State)
        {
            case PanelState.Hide:
                _canvasGroup.alpha = 1;
                break;
        }
        
        State = PanelState.Active;
    }

    public virtual void OnRecycle()
    {
        State = PanelState.Recycle;
        
        _canvasGroup.alpha = 0;
        _canvasGroup.DOFade(1, FadeTime);
    }

    public virtual void OnClose()
    {
        
    }
}