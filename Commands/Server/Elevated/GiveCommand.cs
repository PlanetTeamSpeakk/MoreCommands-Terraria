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
                    .Then(Argument("player", PlayerArgumentType.Player)
                        .Executes(ctx => Execute(ctx, ctx.GetArgument<int>("count")))))));
    }

    private static int Execute(CommandContext<CommandSource> ctx, int count)
    {
        Player player;

        if (ctx.Nodes.Any(node => "player" == node.Node.Name))
            player = ctx.GetArgument<Player>("player");
        else if (!ctx.Source.IsPlayer) throw MCBuiltInExceptions.ReqPlayer.Create();
        else player = ctx.Source.Caller.Player;
        

        player.QuickSpawnItem(ctx.GetArgument<int>("item"), count);
        return 1;
    }
}