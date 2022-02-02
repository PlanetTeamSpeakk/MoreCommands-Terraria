using Brigadier.NET;
using MoreCommands.ArgumentTypes;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class SetTileCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Set a tile somewhere in the world.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("settile")
            .Then(Argument("pos", PositionArgumentType.Pos)
                .Then(Argument("type", IdArgumentType.Tile)
                    .Executes(ctx =>
                    {
                        (float x, float y) = PositionArgumentType.GetPosition(ctx, "pos");
                        Util.ForcePlaceTile((int) x / 16, (int) y / 16, ctx.GetArgument<int>("type"));

                        return 1;
                    }))));
    }
}