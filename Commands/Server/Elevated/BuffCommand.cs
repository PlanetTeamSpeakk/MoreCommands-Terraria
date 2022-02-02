using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;
using MoreCommands.ArgumentTypes;
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
                        .Then(Argument("player", PlayerArgumentType.Player)
                            .Executes(ExecuteAdd)))))
            
            .Then(Literal("clear")
                .Executes(ctx => ExecuteClear(ctx, null))
                .Then(Argument("player", PlayerArgumentType.Player)
                    .Executes(ctx => ExecuteClear(ctx, ctx.GetArgument<Player>("player")))))
            
            .Then(Literal("lookup")
                .Then(Argument("query", Arguments.GreedyString())
                    .Executes(ctx => IdHelper.SearchCommand(ctx, "query", IdType.Buff)))));
    }

    private static int ExecuteAdd(CommandContext<CommandSource> ctx)
    {
        Player toAffect = ctx.Nodes.Any(node => node.Node.Name == "player") ? ctx.GetArgument<Player>("player") : ctx.Source.Player;
        
        int buff = ctx.GetArgument<int>("buff");
        int length = ctx.GetArgument<int>("length");
        toAffect.AddBuff(buff, length * 60);
                            
        Reply(toAffect, $"You have been given the {Lang.GetBuffName(buff)} buff for {length} seconds.");
        if (!ctx.Source.IsPlayer || toAffect != ctx.Source.Player)
            Reply(ctx, $"{toAffect.name} has been given the {Lang.GetBuffName(buff)} buff for {length} seconds.");
        
        return buff;
    }

    private static int ExecuteClear(CommandContext<CommandSource> ctx, Player player)
    {
        Player toClear = player ?? ctx.Source.Player;

        for (int i = 0; i < Player.MaxBuffs; i++)
            toClear.DelBuff(i);
        
        Reply(toClear, "Your buffs have been cleared.");
        if (!ctx.Source.IsPlayer || toClear != ctx.Source.Player)
            Reply(ctx, $"{toClear.name}'s buffs have been cleared.");

        return 1;
    }
}