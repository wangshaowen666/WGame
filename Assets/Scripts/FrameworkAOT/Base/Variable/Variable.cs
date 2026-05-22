/*--------------------------------------------------------------
 * File: Variable.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2025/11/07 18:15:41 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

/// <summary>
/// 纯单次转换，开销大于拆箱、装箱，但是依靠池化操作，后续gc开销为0，主要用于底层接口 多态参数中
/// </summary>
public abstract class Variable : IResetable
{
    public virtual void Reset(){}
}

public sealed class VarBool : Variable<bool>
{
    public static implicit operator VarBool(bool value)
    {
        VarBool varValue = ClassPool.Get<VarBool>();
        varValue.Value = value;
        return varValue;
    }
    
    public static implicit operator bool(VarBool varBool)
    {
        return varBool.Value;
    }
}

public sealed class VarByte : Variable<byte>
{
    public static implicit operator VarByte(byte value)
    {
        VarByte varValue = ClassPool.Get<VarByte>();
        varValue.Value = value;
        return varValue;
    }
    
    public static implicit operator byte(VarByte value)
    {
        return value.Value;
    }
}

public sealed class VarShort : Variable<short>
{
    public static implicit operator VarShort(short value)
    {
        VarShort varValue = ClassPool.Get<VarShort>();
        varValue.Value = value;
        return varValue;
    }
    
    public static implicit operator short(VarShort value)
    {
        return value.Value;
    }
}

public sealed class VarInt : Variable<int>
{
    public static implicit operator VarInt(int value)
    {
        VarInt varValue = ClassPool.Get<VarInt>();
        varValue.Value = value;
        return varValue;
    }
    
    public static implicit operator int(VarInt value)
    {
        return value.Value;
    }
}

public sealed class VarLong : Variable<long>
{
    public static implicit operator VarLong(long value)
    {
        VarLong varValue = ClassPool.Get<VarLong>();
        varValue.Value = value;
        return varValue;
    }
    
    public static implicit operator long(VarLong value)
    {
        return value.Value;
    }
}

public sealed class VarFloat : Variable<float>
{
    public static implicit operator VarFloat(float value)
    {
        VarFloat varValue = ClassPool.Get<VarFloat>();
        varValue.Value = value;
        return varValue;
    }
    
    public static implicit operator float(VarFloat value)
    {
        return value.Value;
    }
}

public sealed class VarDouble : Variable<double>
{
    public static implicit operator VarDouble(double value)
    {
        VarDouble varValue = ClassPool.Get<VarDouble>();
        varValue.Value = value;
        return varValue;
    }
    
    public static implicit operator double(VarDouble value)
    {
        return value.Value;
    }
}