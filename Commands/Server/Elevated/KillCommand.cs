using Brigadier.NET;
using MoreCommands.ArgumentTypes;
using MoreCommands.Misc;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class KillCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Kill anything and everything you wish.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("kill")
            .Then(Argument("npcs", NpcSelectorArgumentType.NpcSelector)
                .Executes(ctx =>
                {
                    // TODO
                    Reply(ctx, "Test");
                    return 1;
                })));
    }
}