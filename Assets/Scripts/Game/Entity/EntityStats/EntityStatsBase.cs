/*--------------------------------------------------------------
 * File: EntityStatsBase.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/05/11 16:12:57 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public class EntityStatsBase : IResetable
{
    public int MaxHp { get; protected set; }
    public int CurrentHp { get; protected set; }
    public bool IsDead { get; protected set; }
    public bool IsInvincible { get; set; }

    public event Action<int, int> OnHpChanged;
    public event Action OnDeath;

    public virtual void Reset()
    {
        MaxHp = 0;
        CurrentHp = 0;
        IsDead = false;
        IsInvincible = false;
        OnHpChanged = null;
        OnDeath = null;
    }

    public void InitHp(int maxHp)
    {
        MaxHp = maxHp;
        CurrentHp = maxHp;
        IsDead = false;
    }

    public virtual int TakeDamage(int rawDamage, EntityStatsBase attacker = null)
    {
        if (IsDead || IsInvincible)
            return 0;

        int actualDamage = Mathf.Max(1, rawDamage);
        CurrentHp = Mathf.Max(0, CurrentHp - actualDamage);
        OnHpChanged?.Invoke(actualDamage, CurrentHp);

        if (CurrentHp <= 0)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }

        return actualDamage;
    }

    public virtual int Heal(int amount)
    {
        if (IsDead)
            return 0;

        int oldHp = CurrentHp;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        int healed = CurrentHp - oldHp;

        if (healed > 0)
            OnHpChanged?.Invoke(-healed, CurrentHp);

        return healed;
    }
}