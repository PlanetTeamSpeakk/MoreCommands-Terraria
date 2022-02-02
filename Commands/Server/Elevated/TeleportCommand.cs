using System.Collections.Generic;
using Brigadier.NET;
using MoreCommands.ArgumentTypes;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class TeleportCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override bool Console => false;
    public override string Description => "Teleport anywhere in the world.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("tp").Fork(
            dispatcher.Register(RootLiteralReq("teleport")
                .Then(Argument("pos", PositionArgumentType.TilePos)
                    .Executes(ctx =>
                    {
                        (float x, float y) = PositionArgumentType.GetPosition(ctx, "pos");
                        Util.Teleport(ctx.Source.Player, ((int) x, (int) y));
                        Reply(ctx, $"You have been teleported to {Coloured($"{(int) x / 16}", SF)}, {Coloured($"{(int) y / 16}", SF)}.");

                        return 1;
                    }))), ctx => new List<CommandSource>(new []{ctx.Source})));
    }
}