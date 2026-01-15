/*--------------------------------------------------------------
 * File: Variable.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/07 18:15:41 
 *--------------------------------------------------------------
 */

using System;
using UnityEngine;

public abstract class Variable : IResetable
{
    /// <summary>
    /// 获取变量类型
    /// </summary>
    public abstract Type Type { get; }
    /// <summary>
    /// 获取变量值
    /// </summary>
    /// <returns>变量值</returns>
    public abstract object GetValue();
    /// <summary>
    /// 设置变量值
    /// </summary>
    /// <param name="value"></param>
    public abstract void SetValue(object value);
    public virtual void Reset(){}
}


// 针对最常用的引用类型提供特化重载，避免包装开销
// public void Process(string value) { ... }
// public void Process(MyClass obj) { ... }

public sealed class VarBool : Variable<bool>
{
    public static implicit operator VarBool(bool value)
    {
        VarBool varValue = ClassFactory.Instance.Get<VarBool>();
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
        VarByte varValue = ClassFactory.Instance.Get<VarByte>();
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
        VarShort varValue = ClassFactory.Instance.Get<VarShort>();
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
        VarInt varValue = ClassFactory.Instance.Get<VarInt>();
        //VarInt varValue = new VarInt();
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
        VarLong varValue = ClassFactory.Instance.Get<VarLong>();
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
        VarFloat varValue = ClassFactory.Instance.Get<VarFloat>();
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
        VarDouble varValue = ClassFactory.Instance.Get<VarDouble>();
        varValue.Value = value;
        return varValue;
    }
    
    public static implicit operator double(VarDouble value)
    {
        return value.Value;
    }
}