using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;
using MoreCommands.ArgumentTypes;
using MoreCommands.ArgumentTypes.Entities;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class GiveCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Gives an item.";

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("give")
            .Then(Argument("item", IdArgumentType.Item)
                .Executes(ctx => Execute(ctx, 1))
                .Then(Argument("count", Arguments.Integer(1))
                    .Executes(ctx => Execute(ctx, ctx.GetArgument<int>("count")))
                    .Then(Argument("players", EntityArgumentType.Players)
                        .Executes(ctx => Execute(ctx, ctx.GetArgument<int>("count")))))));
    }

    private static int Execute(CommandContext<CommandSource> ctx, int count)
    {
        IEnumerable<Player> players;

        if (ctx.Nodes.Any(node => "players" == node.Node.Name))
            players = EntityArgumentType.GetPlayers(ctx, "players");
        else if (!ctx.Source.IsPlayer) throw MCBuiltInExceptions.ReqPlayer.Create();
        else players = Util.Singleton(ctx.Source.Caller.Player);

        players = players.ToList();
        foreach (Player player in players)
            player.QuickSpawnItem(ctx.GetArgument<int>("item"), count);
        
        Reply(ctx, $"Gave {Coloured(count + "x")} {Coloured(IdHelper.GetName(IdType.Item, ctx.GetArgument<int>("item")))} to " +
                   $"{(players.Count() == 1 ? ctx.Source.IsPlayer && players.First() == ctx.Source.Player ? "you" : Coloured(players.First().name) : Coloured(players.Count() + " players"))}.");
        return 1;
    }
}