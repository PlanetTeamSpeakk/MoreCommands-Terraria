using System;
using Brigadier.NET.Builder;

namespace MoreCommands.Extensions;

public static class LiteralArgumentBuilderExtensions
{
    public static LiteralArgumentBuilder<T> AlsoRequires<T>(this LiteralArgumentBuilder<T> self, Predicate<T> requirement)
    {
        Predicate<T> requirementOld = self.Requirement;
        return requirementOld is null ? self.Requires(requirement) : self.Requires(source => requirementOld(source) && requirement(source));
    }
}