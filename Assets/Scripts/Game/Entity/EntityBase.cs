/*--------------------------------------------------------------
 * File: EntityBase.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/27 10:11:11 
 *--------------------------------------------------------------
 */

using UnityEngine;

public abstract class EntityBase : MonoBehaviour
{
    protected int _id;
    public Camp Camp { get; protected set; }
    private bool _updateRegistered;

    public virtual void OnInit(int id)
    {
        _id = id;
        TryRegisterUpdate();
    }

    public virtual void OnRecycle()
    {
        TryUnRegisterUpdate();
    }

    private void OnDestroy()
    {
        TryUnRegisterUpdate();
    }

    protected void TryRegisterUpdate()
    {
        if (!_updateRegistered && this is IUpdateable updatable)
        {
            CoreMgr.Update.RegisterUpdate(updatable);
            _updateRegistered = true;
        }
    }

    protected void TryUnRegisterUpdate()
    {
        if (_updateRegistered && this is IUpdateable updatable)
        {
            CoreMgr.Update.UnRegisterUpdate(updatable);
            _updateRegistered = false;
        }
    }
}