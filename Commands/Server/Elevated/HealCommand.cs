using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Context;
using Microsoft.Xna.Framework;
using MoreCommands.ArgumentTypes;
using MoreCommands.ArgumentTypes.Entities;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class HealCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Magically replenishes your health and mana.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("heal")
            .Executes(ctx => Execute(ctx, null))
            .Then(Argument("players", EntityArgumentType.Players)
                .Executes(ctx => Execute(ctx, EntityArgumentType.GetPlayers(ctx, "players")))));
    }

    private static int Execute(CommandContext<CommandSource> ctx, IEnumerable<Player> players)
    {
        IEnumerable<Player> toHeal = (players ?? Util.Singleton(ctx.Source.Player));
        
        foreach (Player player in toHeal)
        {
            player.statLife = player.statLifeMax;
            player.statMana = player.statManaMax;
            Reply(player, "You have been healed!", Color.Green);
        }
        
        if (!ctx.Source.IsPlayer || !toHeal.Contains(ctx.Source.Player))
            Reply(ctx, $"{(toHeal.Count() == 1 ? Coloured(toHeal.First().name) + " has" : Coloured(toHeal.Count()) + " players have")} been healed.", Color.Green);

        return 1;
    }
}