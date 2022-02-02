using Brigadier.NET;
using MoreCommands.ArgumentTypes;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Unelevated;

public class TimeCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Get or set the time of the world.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("time")
            .Executes(ctx =>
            {
                (int hour, int minute) = Util.GetTime();
                
                Reply(ctx, $"The current time is {hour}:{minute:D2}.");
                return hour * 60 + minute;
            })
            .Then(LiteralReq("day")
                .Executes(ctx =>
                {
                    Util.SetTime(4, 30);
        
                    Reply(ctx, "The time has been set to day.");
                    return 1;
                }))
            .Then(LiteralReq("noon")
                .Executes(ctx =>
                {
                    Util.SetTime(12, 00);
        
                    Reply(ctx, "The time has been set to noon.");
                    return 1;
                }))
            .Then(LiteralReq("night")
                .Executes(ctx =>
                {
                    Util.SetTime(19, 30);

                    Reply(ctx, "The time has been set to night.");
                    return 1;
                }))
            .Then(LiteralReq("midnight")
                .Executes(ctx =>
                {
                    Util.SetTime(0, 00);
        
                    Reply(ctx, "The time has been set to midnight.");
                    return 1;
                }))
            .Then(Argument("time", TimeArgumentType.Time)
                .Requires(IsOp)
                .Executes(ctx =>
                {
                    (int hour, int minute) = ctx.GetArgument<(int hour, int minute)>("time");
                    
                    Util.SetTime(hour, minute);
                    Reply(ctx, $"The time has been set to {hour}:{minute:D2}");
                    return 1;
                })));
    }
}