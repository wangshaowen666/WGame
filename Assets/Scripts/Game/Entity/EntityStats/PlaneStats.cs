/*--------------------------------------------------------------
 * File: ShipStats.cs
 * Author: Wang ShaoWen
 * Time: 2026/05/11 16:13:28 
 *--------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using cfg;
using UnityEngine;

// public enum BuffStatType
// {
//     Attack,
//     Defense,
//     Speed,
// }
//
// public enum BuffOpType
// {
//     Add,
//     Multiply,
// }
//
// public struct BuffModifier
// {
//     public int BuffId;
//     public BuffStatType StatType;
//     public BuffOpType OpType;
//     public float Value;
//     public float Duration;
//     public float RemainTime;
// }

public class PlaneStats : EntityStatsBase
{
    public float ModifiedAttack { get; private set; }
    public float ModifiedDefense { get; private set; }
    public float ModifiedSpeed { get; private set; }

    private DPlane _cfg;
    //private List<BuffModifier> _buffs;

    public override void Reset()
    {
        base.Reset();
        _cfg = null;
        //_buffs?.Clear();
        ModifiedAttack = 0;
        ModifiedDefense = 0;
        ModifiedSpeed = 0;
    }

    public void Init(DPlane cfg)
    {
        _cfg = cfg;
        //_buffs ??= new List<BuffModifier>();
        InitHp(cfg.MaxHp);
        RecalculateStats();
    }

    // public void ApplyBuff(int buffId, BuffStatType statType, BuffOpType opType, float value, float duration)
    // {
    //     RemoveBuff(buffId);
    //
    //     _buffs.Add(new BuffModifier
    //     {
    //         BuffId = buffId,
    //         StatType = statType,
    //         OpType = opType,
    //         Value = value,
    //         Duration = duration,
    //         RemainTime = duration,
    //     });
    //
    //     RecalculateStats();
    // }
    //
    // public void RemoveBuff(int buffId)
    // {
    //     for (int i = _buffs.Count - 1; i >= 0; i--)
    //     {
    //         if (_buffs[i].BuffId == buffId)
    //         {
    //             _buffs.RemoveAt(i);
    //             break;
    //         }
    //     }
    //     RecalculateStats();
    // }
    //
    // public void UpdateBuffs(float deltaTime)
    // {
    //     bool changed = false;
    //     for (int i = _buffs.Count - 1; i >= 0; i--)
    //     {
    //         var buff = _buffs[i];
    //         buff.RemainTime -= deltaTime;
    //         if (buff.RemainTime <= 0)
    //         {
    //             _buffs.RemoveAt(i);
    //             changed = true;
    //         }
    //         else
    //         {
    //             _buffs[i] = buff;
    //         }
    //     }
    //
    //     if (changed)
    //         RecalculateStats();
    // }

    public int GetAttackDamage()
    {
        return Mathf.FloorToInt(ModifiedAttack);
    }

    public override int TakeDamage(int rawDamage, EntityStatsBase attacker = null)
    {
        if (IsDead || IsInvincible)
            return 0;

        float defense = ModifiedDefense;
        int finalDamage = Mathf.Max(1, Mathf.FloorToInt(rawDamage * (100f / (100f + defense))));

        return base.TakeDamage(finalDamage, attacker);
    }

    private void RecalculateStats()
    {
        if (_cfg == null)
            return;
        
        float attackAdd = 0, attackMul = 0;
        float defenseAdd = 0, defenseMul = 0;
        float speedAdd = 0, speedMul = 0;
        
        // foreach (var buff in _buffs)
        // {
        //     switch (buff.StatType)
        //     {
        //         case BuffStatType.Attack:
        //             if (buff.OpType == BuffOpType.Add) attackAdd += buff.Value;
        //             else attackMul += buff.Value;
        //             break;
        //         case BuffStatType.Defense:
        //             if (buff.OpType == BuffOpType.Add) defenseAdd += buff.Value;
        //             else defenseMul += buff.Value;
        //             break;
        //         case BuffStatType.Speed:
        //             if (buff.OpType == BuffOpType.Add) speedAdd += buff.Value;
        //             else speedMul += buff.Value;
        //             break;
        //     }
        // }
        
        ModifiedAttack = (_cfg.Attack + attackAdd) * (1f + attackMul);
        ModifiedDefense = (_cfg.Defense + defenseAdd) * (1f + defenseMul);
        ModifiedSpeed = (_cfg.Speed + speedAdd) * (1f + speedMul);
    }
}