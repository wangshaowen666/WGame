/*--------------------------------------------------------------
 * File: UIPanelBase.cs
 * Author: Wang ShaoWen
 * Time: 2025/12/31 18:48:58 
 *--------------------------------------------------------------
 */

using cfg;
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
public class UIPanelBase : MonoBehaviour
{
    public PanelState State { get; private set; }
    public DPnlId PnlId => _cfg.Id;
    public bool IsHideCovered => _cfg.HideCovered;
    
    private const float FadeTime = 0.3f;
    
    private CanvasGroup _canvasGroup;
    private DUIPanel _cfg;

   

    public virtual void OnInit(DUIPanel cfg)
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _cfg = cfg;
    }

    public virtual void OnOpen(int serialId, object userData)
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
                _canvasGroup.DOFade(1, FadeTime);
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