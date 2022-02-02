using Brigadier.NET;
using Microsoft.Xna.Framework;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Unelevated;

public class DropStoreCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override bool Console => false;
    public override string Description => "Clears your inventory and stores its contents in a chest at your feet.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("dropstore")
            .Executes(ctx =>
            {
                Point pos = ctx.Source.Player.Center.ToTileCoordinates();

                Chest[] chests = new Chest[2];
                for (int i = 0; i < 2; i++)
                {
                    TileObject.Place(new TileObject
                    {
                        xCoord = pos.X + i * 2,
                        yCoord = pos.Y,
                        type = TileID.Containers
                    });
                    chests[i] = Main.chest[Chest.CreateChest(pos.X + i * 2, pos.Y)];
                }

                for (int i = 0; i < ctx.Source.Player.inventory.Length; i++)
                {
                    Chest chest = chests[i < 40 ? 0 : 1];

                    chest.item[i % 40] = ctx.Source.Player.inventory[i].Clone();
                    ctx.Source.Player.inventory[i] = new Item();
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.ChestUpdates, number2: pos.X, number3: pos.Y);
                    NetMessage.SendData(MessageID.ChestUpdates, number2: pos.X + 2, number3: pos.Y);
                }

                Reply(ctx, "Your items have been dumped in a chest at your feet.");
                return 1;
            }));
    }
}