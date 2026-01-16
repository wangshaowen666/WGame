/*--------------------------------------------------------------
 * File: GenericVariable.cs
 * Author: Wang ShaoWen
 * Time: 2025/11/07 18:21:31 
 *--------------------------------------------------------------
 */

using System;

public abstract class Variable<T> : Variable
{
    private T _value;

    public T Value
    {
        get
        {
            return _value;
        }
        set
        {
            _value = value;
        }
    }

    public override string ToString()
    {
        return (_value == null ? "<Null>" : _value.ToString());
    }
}