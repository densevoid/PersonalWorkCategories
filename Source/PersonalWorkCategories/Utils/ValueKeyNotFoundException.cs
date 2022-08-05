using System;
using System.Collections.Generic;

namespace HandyUI_PersonalWorkCategories.Utils;

public class ValueKeyNotFoundException : KeyNotFoundException
{
    public ValueKeyNotFoundException(object key, string message) : this(key, message, null)
    {
    }

    public ValueKeyNotFoundException(object key, string message, Exception innerException) : base(message,
        innerException)
    {
        Key = key;
    }

    public object Key { get; }

    public override string ToString()
    {
        return $"Key [{Key}] not found in the dictionary.\n";
    }
}