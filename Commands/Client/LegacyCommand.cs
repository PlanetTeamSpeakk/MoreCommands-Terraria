using System.Linq;
using Brigadier.NET;
using MoreCommands.Misc;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class LegacyCommand : Command
{
    public override CommandType Type => CommandType.Chat;
    public override bool Console => true;
    public override string Description => "List all legacy commands (from vanilla Terraria and other mods)";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("legacy")
            .Executes(ctx =>
            {
                // Distinct call so modcommands overwriting vanilla commands don't cause the command to be listed twice.
                Reply(ctx, $"The following legacy commands have been registered: {JoinNicelyColoured(MoreCommands.LegacyCommands.Distinct())}.");
                return MoreCommands.LegacyCommands.Count();
            }));
    }
}