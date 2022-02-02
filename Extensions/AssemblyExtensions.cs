using System;
using System.Linq;
using System.Reflection;

namespace MoreCommands.Extensions;

public static class AssemblyExtensions
{
    public static Type[] GetTypesIncludingNested(this Assembly self) => 
        self.GetTypes()
            .SelectMany(t => new[] { t }
                .Concat(t.GetNestedTypes()))
            .ToArray();
}