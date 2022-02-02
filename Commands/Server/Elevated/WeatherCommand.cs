using Brigadier.NET;
using MoreCommands.Misc;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class WeatherCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Manipulate the weather to your will.";

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("weather")
            .Then(Literal("clear")
                .Executes(ctx =>
                {
                    Main.StopRain();

                    Reply(ctx, "Can't you tell I got news for you? The sun is shining and so are you.");
                    return 1;
                }))
            .Then(Literal("rain")
                .Executes(ctx =>
                {
                    Main.StartRain();
                    
                    Reply(ctx, "Purple rain, purple raaaaiiiinn");
                    return 1;
                })));
    }
}