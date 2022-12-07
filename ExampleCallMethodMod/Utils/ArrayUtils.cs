using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ExampleCallMethodMod.Utils;

public static class ArrayUtils
{
    public const string NullString = "<null>";

    public static IEnumerable<object> ToArray(object obj)
    {
        IEnumerable asArray = obj as IEnumerable;
        if (asArray == null)
        {
            if (obj == null)
            {
                return Enumerable.Empty<object>();
            }
            return new[] { obj };
        }
        return asArray.Cast<object>();
    }

    public static IEnumerable<string> ToStringArray(object obj)
    {
        return ToArray(obj).Select(o => o?.ToString() ?? NullString);
    }
}