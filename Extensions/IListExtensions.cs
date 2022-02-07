using System;
using System.Collections.Generic;

namespace MoreCommands.Extensions;

// ReSharper disable once InconsistentNaming
public static class IListExtensions
{
    public static T GetRandom<T>(this IList<T> self, int? seed = null)
    {
        if (self.Count == 0) throw new IndexOutOfRangeException();
        
        Random random = seed is null ? new Random() : new Random(seed.Value);
        return self[random.Next(self.Count)];
    }
    
    public static void Shuffle<T>(this IList<T> list)
    {
        Random rng = new();
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            (list[k], list[n]) = (list[n], list[k]);
        }  
    }
}