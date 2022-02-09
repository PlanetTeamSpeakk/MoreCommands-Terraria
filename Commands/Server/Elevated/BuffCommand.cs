using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Context;
using MoreCommands.ArgumentTypes;
using MoreCommands.ArgumentTypes.Entities;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class BuffCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Manipulate your (de)buffs.";

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("buff")
            .Then(Literal("add")
                .Then(Argument("buff", IdArgumentType.Buff)
                    .Then(Argument("length", Arguments.Integer(1))
                        .Executes(ExecuteAdd)
                        .Then(Argument("players", EntityArgumentType.Players)
                            .Executes(ExecuteAdd)))))
            
            .Then(Literal("clear")
                .Executes(ctx => ExecuteClear(ctx, null))
                .Then(Argument("players", EntityArgumentType.Players)
                    .Executes(ctx => ExecuteClear(ctx, EntityArgumentType.GetPlayers(ctx, "players")))))
            
            .Then(Literal("lookup")
                .Then(Argument("query", Arguments.GreedyString())
                    .Executes(ctx => IdHelper.SearchCommand(ctx, "query", IdType.Buff)))));
    }

    private static int ExecuteAdd(CommandContext<CommandSource> ctx)
    {
        IEnumerable<Player> toAffect = (ctx.Nodes.Any(node => node.Node.Name == "players") ? EntityArgumentType.GetPlayers(ctx, "players") : Util.Singleton(ctx.Source.Player)).ToList();
        
        int buff = ctx.GetArgument<int>("buff");
        int length = ctx.GetArgument<int>("length");
        foreach (Player player in toAffect)
        {
            player.AddBuff(buff, length * 60);
            Reply(player, $"You have been given the {Lang.GetBuffName(buff)} buff for {length} seconds.");
        }
        
        if (!ctx.Source.IsPlayer || !toAffect.Contains(ctx.Source.Player))
            Reply(ctx, $"{(toAffect.Count() == 1 ? Coloured(toAffect.First().name) + " has" : Coloured(toAffect.Count()) + " players have")} been given the " +
                       $"{Coloured(Lang.GetBuffName(buff))} buff for {Coloured(length)} seconds.");
        
        return buff;
    }

    private static int ExecuteClear(CommandContext<CommandSource> ctx, IEnumerable<Player> players)
    {
        IEnumerable<Player> toClear = (players ?? Util.Singleton(ctx.Source.Player)).ToList();

        foreach (Player player in toClear)
        {
            for (int i = 0; i < Player.MaxBuffs; i++)
                player.DelBuff(i);
            
            Reply(player, "Your buffs have been cleared.");
        }

        if (!ctx.Source.IsPlayer || !toClear.Contains(ctx.Source.Player))
            Reply(ctx, $"{Coloured(toClear.Count() == 1 ? toClear.First().name + " has" : toClear.Count() + " players have")}'s buffs have been cleared.");

        return 1;
    }
}