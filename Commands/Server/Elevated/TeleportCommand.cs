using Brigadier.NET;
using MoreCommands.ArgumentTypes;
using MoreCommands.ArgumentTypes.Entities;
using MoreCommands.Extensions;
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
                        (int x, int y) pos = PositionArgumentType.GetPositionI(ctx, "pos");
                        Util.Teleport(ctx.Source.Player, pos);
                        
                        Reply(ctx, $"You have been teleported to {Coloured(pos.x / 16)}, {Coloured(pos.y / 16)}.");
                        return 1;
                    }))
                .Then(Argument("target", EntityArgumentType.Entity)
                    .Executes(ctx =>
                    {
                        Entity target = EntityArgumentType.GetEntity(ctx, "target");
                        Util.Teleport(ctx.Source.Player, target.position.ToIntTuple());
                        
                        Reply(ctx, $"You have been teleported to {Coloured(target.GetName())}.");
                        return 1;
                    }))), ctx => Util.Singleton(ctx.Source)));
    }
}