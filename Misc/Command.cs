using System;
using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;
using Microsoft.Xna.Framework;
using MoreCommands.Extensions;
using MoreCommands.Misc.Styling;
using Brigadier.NET.ArgumentTypes;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MoreCommands.Misc;

public abstract class Command
{
    public abstract CommandType Type { get; }
    public virtual bool Console => Type != CommandType.Chat;
    public abstract string Description { get; }
    public virtual bool ServerOnly => false;
    public virtual bool IgnoreAmbiguities => false;
    protected static Color DF => MoreCommands.DefColour;
    protected static Color SF => MoreCommands.SecColour;
    protected static Predicate<CommandSource> IsOp => source => source.IsOp;
    
    internal virtual void Init() {}

    public abstract void Register(CommandDispatcher<CommandSource> dispatcher);

    public virtual void OnUpdate() {}

    protected LiteralArgumentBuilder<CommandSource> RootLiteral(string literal) => LiteralArgumentBuilder<CommandSource>.LiteralArgument(literal)
        .Requires(source => source.Caller.CommandType == Type || Type == CommandType.World && source.Caller.CommandType ==
            (Main.netMode == NetmodeID.SinglePlayer ? CommandType.Chat : CommandType.Server) || source.Caller.CommandType == CommandType.Console && Console);

    protected static LiteralArgumentBuilder<CommandSource> Literal(string literal) => LiteralArgumentBuilder<CommandSource>.LiteralArgument(literal);

    protected LiteralArgumentBuilder<CommandSource> RootLiteralReq(string literal) => RootLiteral(literal).AlsoRequires(IsOp);
    
    protected static LiteralArgumentBuilder<CommandSource> LiteralReq(string literal) => Literal(literal).Requires(IsOp);

    protected static RequiredArgumentBuilder<CommandSource, T> Argument<T>(string name, ArgumentType<T> argumentType) => RequiredArgumentBuilder<CommandSource, T>.RequiredArgument(name, argumentType);

    protected static void Reply(CommandContext<CommandSource> ctx, string message) => Reply(ctx, message, DF);

    protected static void Reply(CommandContext<CommandSource> ctx, string message, Color color) => ctx.Source.Reply(message, color);

    protected static void Error(CommandContext<CommandSource> ctx, string message) => ctx.Source.Error(message);
    
    protected static void Reply(Player player, string message) => Reply(player, message, MoreCommands.DefColour);

    protected static void Reply(Player player, string message, Color color) => ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(message), color, player.whoAmI);

    protected static void Error(Player player, string message) => Reply(player, message, Color.Red);

    public static string JoinNicely(IEnumerable<string> strings)
    {
        string[] stringsArray = strings.ToArray();

        string result = "";
        for (int i = 0; i < stringsArray.Length; i++)
            result += stringsArray[i] + (i == stringsArray.Length - 2 ? " and " : ", ");

        return result[..^2];
    }

    protected static string JoinNicelyColoured(IEnumerable<string> strings, Color? colour = null) => JoinNicely(strings.Select(s => Coloured(s, colour ?? SF)));

    protected static string Coloured<T>(T t, Color? colour = null)
    {
        if (t is null) return "null";
        string s = t.ToString();
        colour ??= SF;
        
        return $"[c/{colour.Value.R:X2}{colour.Value.G:X2}{colour.Value.B:X2}:{s}]";
    }
    
    protected static StyledTextBuilder Styled<T>(T text) => StyledTextBuilder.Builder(text);
}