using System.Linq;
using Brigadier.NET.Context;
using Brigadier.NET.Tree;
using MoreCommands.Misc;

namespace MoreCommands.Extensions;

public static class CommandContextExtensions
{
    public static T GetArgumentOrDefault<T>(this CommandContext<CommandSource> ctx, string argName, T def = default) => 
        ctx.Nodes.Any(node => node.Node is ArgumentCommandNode<CommandSource, T> && node.Node.Name == argName) ? 
            ctx.GetArgument<T>(argName) : def;
}