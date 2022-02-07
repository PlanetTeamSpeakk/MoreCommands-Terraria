using Brigadier.NET;
using MoreCommands.Misc;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class CSilentCommand : Command
{
    public override CommandType Type => CommandType.Chat;
    public override string Description => "Run a command silently on the client.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("csilent").Redirect(dispatcher.GetRoot(), ctx => ctx.Source.WithSilent(true)));
    }
}