using Brigadier.NET;
using MoreCommands.Misc;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Unelevated;

public class SuicideCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override bool Console => false;
    public override string Description => "Kill yourself for whatever reason.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("suicide")
            .Executes(ctx =>
            {
                ctx.Source.Player.KillMe(PlayerDeathReason.ByCustomReason($"{ctx.Source.Player.name} has taken their own life."), double.MaxValue, 0);
                return 1;
            }));
    }
}