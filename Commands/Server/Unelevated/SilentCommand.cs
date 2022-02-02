using Brigadier.NET;
using MoreCommands.Misc;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Unelevated;

public class SilentCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Run a command silently.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("silent").Redirect(dispatcher.GetRoot(), ctx => ctx.Source.WithSilent(true)));
    }
}