/*--------------------------------------------------------------
 * File: BattlePanel.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/05/12 14:28:45
 *--------------------------------------------------------------
 */

using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class BattlePanel : UIPanelBase, IUpdateable
{
    private PlayerPlane _playerPlane;
    private Camera _camera;
    private float _planeDepth;

    public override void OnOpen(object userData = null)
    {
        base.OnOpen(userData);
        _camera = Camera.main;
        EnhancedTouchSupport.Enable();
        UpdateMgr.RegisterUpdate(this);

        if (GameMgr.Battle.CurrentBattle is BattleSurvival battle)
        {
            _playerPlane = battle.PlayerPlane;
            if (_playerPlane != null && _camera != null)
            {
                Vector3 planePos = _playerPlane.transform.position;
                _planeDepth = _camera.WorldToScreenPoint(planePos).z;
            }
        }
    }

    public void MyUpdate(float deltaTime, float realDeltaTime)
    {
        if (_playerPlane == null || _playerPlane.Stats.IsDead)
            return;

        Vector3? worldTarget = GetTouchWorldPosition();
        if (worldTarget.HasValue)
        {
            _playerPlane.SetMoveTarget(worldTarget.Value);
        }
    }

    public override void OnRecycle()
    {
        base.OnRecycle();
        EnhancedTouchSupport.Disable();
        UpdateMgr.UnRegisterUpdate(this);
    }

    private void OnDestroy()
    {
        UpdateMgr.UnRegisterUpdate(this);
    }

    private Vector3? GetTouchWorldPosition()
    {
        if (Touch.activeTouches.Count == 0)
            return null;

        var touch = Touch.activeTouches[0];
        if (touch.phase != TouchPhase.Began &&
            touch.phase != TouchPhase.Moved &&
            touch.phase != TouchPhase.Stationary)
            return null;

        Vector3 screenPos = touch.screenPosition;
        screenPos.z = _planeDepth;
        return _camera.ScreenToWorldPoint(screenPos);
    }
}