using Brigadier.NET;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class JumpCommand : Command
{
    public override CommandType Type => CommandType.Chat;
    public override string Description => "Teleport where your cursor is.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("jump")
            .Executes(ctx =>
            {
                Util.SendCommand($"/{(ctx.Source.IsSilent ? "silent " : "")}teleport {Main.MouseWorld.X / 16} {Main.MouseWorld.Y / 16}");
                return 1;
            }));
    }
}