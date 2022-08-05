using System.Collections.Generic;

namespace HandyUI_PersonalWorkCategories.Utils;

public static class GetKeySafelyUtil
{
    public static TValue GetSafety<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key)
    {
        try
        {
            return source[key];
        }
        catch (KeyNotFoundException ex)
        {
            throw new ValueKeyNotFoundException(key, ex.Message, ex.InnerException);
        }
    }
}