using Brigadier.NET;
using MoreCommands.Misc;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class InvasionCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Starts an invasion.";
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("invasion")
            .Then(Argument("invasion", Arguments.Integer(1))
                .Executes(ctx =>
                {
                    int invasion = ctx.GetArgument<int>("invasion");
                    Main.StartInvasion(invasion);

                    Reply(ctx, $"An invasion of type {invasion} has started.");
                    return invasion;
                }))
            .Then(Literal("stop")
                .Executes(ctx =>
                {
                    if (Main.invasionType != 0)
                    {
                        Error(ctx, "There is no ongoing invasion.");
                        return 0;
                    }
                    
                    Main.StartInvasion(0);
                    
                    Reply(ctx, "The ongoing invasion has been stopped.");
                    return 1;
                })));
    }
}