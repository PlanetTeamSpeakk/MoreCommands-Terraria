using Brigadier.NET;
using MoreCommands.Misc;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class RevealMapCommand : Command
{
    public override CommandType Type => CommandType.Chat;
    public override string Description => "Reveals the entire map on singleplayer.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("revealmap")
            .Executes(ctx =>
            {
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    Error(ctx, "This command may only be used on singleplayer.");
                    return 0;
                }
                
                for (int x = 0; x < Main.maxTilesX; x++)
                    for (int y = 0; y < Main.maxTilesY; y++)
                        Main.Map.Update(x, y, byte.MaxValue);

                Main.refreshMap = true;
                Reply(ctx, "The whole world is now at your feet.");
                return 1;
            }));
    }
}