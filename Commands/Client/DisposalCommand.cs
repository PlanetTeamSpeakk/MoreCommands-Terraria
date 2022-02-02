using Brigadier.NET;
using MoreCommands.Misc;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace MoreCommands.Commands.Client;

public class DisposalCommand : Command
{
    public static int Disposal { get; private set; }
    public override CommandType Type => CommandType.Chat;
    public override string Description => "A handy mobile chest that destroys everything you put into it.";

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("disposal")
            .Executes(ctx =>
            {
                if (ctx.Source.Player.chest != -1 && Main.myPlayer == ctx.Source.Player.whoAmI)
                {
                    for (int index = 0; index < 40; ++index)
                        ItemSlot.SetGlow(index, -1f, true);
                }
                
                int chest = Chest.FindEmptyChest(-1, -1);
                if (chest == -1)
                {
                    Error(ctx, "There are too many chests in this world to create a disposal.");
                    return 0;
                }

                Main.chest[chest] = new Chest
                {
                    x = (int) ctx.Source.Pos.X,
                    y = (int) ctx.Source.Pos.Y
                };
                for (int index = 0; index < 40; ++index)
                    Main.chest[chest].item[index] = new Item();

                Main.LocalPlayer.chest = Disposal = chest;
                Main.LocalPlayer.chestX = (int) ctx.Source.Pos.X;
                Main.LocalPlayer.chestY = (int) ctx.Source.Pos.Y;
                
                Main.playerInventory = true;
                Main.recBigList = false;
                
                if (PlayerInput.GrappleAndInteractAreShared)
                    PlayerInput.Triggers.JustPressed.Grapple = false;

                return 1;
            }));
    }

    public override void OnUpdate()
    {
        if (Main.LocalPlayer.chest == Disposal) return;
        
        Main.chest[Disposal] = null;
        Disposal = 0;
    }
}