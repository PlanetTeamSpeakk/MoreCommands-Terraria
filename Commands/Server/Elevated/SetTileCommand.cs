using Brigadier.NET;
using Brigadier.NET.Context;
using MoreCommands.ArgumentTypes;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace MoreCommands.Commands.Server.Elevated;

public class SetTileCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Set a tile somewhere in the world.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("settile")
            .Then(Argument("pos", PositionArgumentType.TilePos)
                .Then(Argument("type", IdArgumentType.Tile)
                    .Executes(ctx => Execute(ctx))
                    .Then(Argument("style", Arguments.Integer(0))
                        .Executes(ctx => Execute(ctx, ctx.GetArgument<int>("style")))
                        .Then(Argument("alternate", Arguments.Integer(0))
                            .Executes(ctx => Execute(ctx, ctx.GetArgument<int>("style"), ctx.GetArgument<int>("alternate"))))))));
    }

    private static int Execute(CommandContext<CommandSource> ctx, int? style = null, int? alternate = null)
    {
        (int x, int y) = PositionArgumentType.GetPositionI(ctx, "pos");
        int type = ctx.GetArgument<int>("type");
        bool b = Util.ForcePlaceTile(x, y, type, style ?? 0, alternate ?? 0);

        if (b) Reply(ctx, $"Tile of type {Coloured(type)} ({Coloured(IdHelper.GetName(IdType.Tile, type))}){(style == null ? "" : " and style " + Coloured(style))} " +
                          $"was placed at {Coloured(x)}, {Coloured(y)}.");
        else Error(ctx, "The tile could not be placed.");
        return 1;
    }
}