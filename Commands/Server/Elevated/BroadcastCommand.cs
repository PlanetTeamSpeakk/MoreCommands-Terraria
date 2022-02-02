using Brigadier.NET;
using Microsoft.Xna.Framework;
using MoreCommands.Misc;
using Terraria.Chat;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class BroadcastCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Send a chat message.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("broadcast")
            .Then(Argument("message", Arguments.GreedyString())
                .Executes(ctx =>
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(ctx.GetArgument<string>("message")), Color.White);
                    return 1;
                })));
    }
}