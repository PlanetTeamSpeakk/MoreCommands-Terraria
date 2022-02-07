using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using MoreCommands.ArgumentTypes;
using MoreCommands.ArgumentTypes.Entities;
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
            .Then(Argument("players", EntityArgumentType.Players)
                .Executes(ctx =>
                {
                    IEnumerable<Player> players = EntityArgumentType.GetPlayers(ctx, "players").ToList();

                    foreach (Player player in players)
                    {
                        player.Spawn(PlayerSpawnContext.RecallFromItem);
                        Reply(player, "You have been sent to spawn.");
                    }

                    Reply(ctx, $"{(players.Count() == 1 ? Coloured(players.First().name) + " has" : Coloured(players.Count() + " players") + " have")} been sent to spawn.");
                    return 1;
                })));
    }
}