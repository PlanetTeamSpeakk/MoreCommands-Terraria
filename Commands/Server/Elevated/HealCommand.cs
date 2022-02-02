using Brigadier.NET;
using Brigadier.NET.Context;
using Microsoft.Xna.Framework;
using MoreCommands.ArgumentTypes;
using MoreCommands.Misc;
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
            .Then(Argument("player", PlayerArgumentType.Player)
                .Executes(ctx => Execute(ctx, ctx.GetArgument<Player>("player")))));
    }

    private static int Execute(CommandContext<CommandSource> ctx, Player player)
    {
        Player toHeal = player ?? ctx.Source.Player;
        
        toHeal.statLife = toHeal.statLifeMax;
        toHeal.statMana = toHeal.statManaMax;
        
        Reply(toHeal, "You have been healed!", Color.Green);
        if (!ctx.Source.IsPlayer || toHeal != ctx.Source.Player)
            Reply(ctx, $"Player {toHeal.name} has been healed.", Color.Green);

        return 1;
    }
}