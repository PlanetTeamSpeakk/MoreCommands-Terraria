using System;
using System.Collections.Generic;

namespace MoreCommands.Extensions;

public static class DictionaryExtensions
{
    // Kudos to Java as they have this by default.
    public static TValue ComputeIfAbsent<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> mapper)
    {
        if (dict.ContainsKey(key))
            return dict[key];

        TValue value = mapper(key);
        dict[key] = value;

        return value;
    }

    public static void AddAll<TKey, TValue, TKeyOther, TValueOther>(this IDictionary<TKey, TValue> dict, IDictionary<TKeyOther, TValueOther> other) where TKeyOther : TKey where TValueOther : TValue
    {
        foreach ((TKeyOther key, TValueOther value) in other)
            dict[key] = value;
    }
}