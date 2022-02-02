using Brigadier.NET;
using MoreCommands.Misc;
using MoreCommands.Misc.Styling;
using MoreCommands.Misc.TagHandlers;
using MoreCommands.Utils;
using ReLogic.OS;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class SeedCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Gives the seed of the world.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("seed")
            .Executes(ctx =>
            {
                WorldFileData world = Main.ActiveWorldFileData;
        
                Reply(ctx, $"The seed of this world is {Styled(world.Seed).WithClick(ClickAction.Copy(world.GetFullSeedText())).WithHover("Click to copy").WithColour(SF)} " +
                           $"({Coloured(world.GetFullSeedText())}), its size is {Coloured(world.WorldSizeName)} ({Coloured(world.WorldSizeX)}\u00D7{Coloured(world.WorldSizeY)}), " + 
                           $"its difficulty is {Coloured(Util.GetGameModeName(world.GameMode))} and its evil type is {Coloured(world.HasCorruption ? "corruption" : world.HasCrimson ? "crimson" : "???")}.");
                
                if (Main.netMode != NetmodeID.Server)
                    Platform.Get<IClipboard>().Value = world.GetFullSeedText();

                return world.Seed;
            }));
    }
}