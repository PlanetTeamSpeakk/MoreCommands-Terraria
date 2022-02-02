using Brigadier.NET;
using MoreCommands.Misc;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class ClearCommand : Command
{
    public override CommandType Type => CommandType.Chat;
    public override string Description => "Clears your inventory, including coins and ammo.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("clear")
            .Executes(ctx =>
            {
                for (int i = 0; i < ctx.Source.Player.inventory.Length; i++)
                    ctx.Source.Player.inventory[i] = new Item();

                Reply(ctx, "Your inventory has been cleared.");
                return 1;
            }));
    }
}