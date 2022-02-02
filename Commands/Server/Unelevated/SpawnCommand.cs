using Brigadier.NET;
using MoreCommands.ArgumentTypes;
using MoreCommands.Misc;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Unelevated;

public class SpawnCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Send yourself or someone else to spawn.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("spawn")
            .Executes(ctx =>
            {
                ctx.Source.Player.Spawn(PlayerSpawnContext.RecallFromItem);

                Reply(ctx, "You have been sent to spawn.");
                return 1;
            })
            .Then(Argument("player", PlayerArgumentType.Player)
                .Executes(ctx =>
                {
                    Player player = ctx.GetArgument<Player>("player");
                    
                    player.Spawn(PlayerSpawnContext.RecallFromItem);
                    Reply(player, "You have been sent to spawn.");
                    
                    Reply(ctx, $"{player.name} has been sent to spawn.");
                    return 1;
                })));
    }
}