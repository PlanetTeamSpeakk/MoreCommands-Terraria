using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Context;
using MoreCommands.ArgumentTypes.Entities;
using MoreCommands.Extensions;
using MoreCommands.Misc;
using MoreCommands.UI;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class TitleCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Send a title message to players.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("title")
            .Then(Argument("players", EntityArgumentType.Players)
                .Then(Argument("title", Arguments.String())
                    .Executes(Execute)
                    .Then(Argument("subtitle", Arguments.String())
                        .Executes(Execute)
                        .Then(Argument("duration", Arguments.Integer(500))
                            .Executes(Execute)
                            .Then(Argument("fade-in", Arguments.Integer(0))
                                .Executes(Execute)
                                .Then(Argument("fade-out", Arguments.Integer(0))
                                    .Executes(Execute))))))));
    }

    private static int Execute(CommandContext<CommandSource> ctx)
    {
        IEnumerable<Player> players = EntityArgumentType.GetPlayers(ctx, "players");
        string title = ctx.GetArgument<string>("title");
        string subtitle = ctx.GetArgumentOrDefault("subtitle", "");
        uint duration = (uint) ctx.GetArgumentOrDefault("duration", 10000);
        uint fadeIn = (uint) ctx.GetArgumentOrDefault("fade-in", 250);
        uint fadeOut = (uint) ctx.GetArgumentOrDefault("fade-out", 250);

        if (Main.netMode == NetmodeID.Server)
        {
            ModPacket packet = MoreCommands.Instance.GetPacket();
            packet.Write((byte) MCPacketID.S2C.Title);
            packet.Write(title);
            packet.Write(subtitle);
            packet.Write(duration);
            packet.Write(fadeIn);
            packet.Write(fadeOut);

            foreach (Player player in players)
                packet.Send(player.whoAmI);
        }
        else TitleUI.ShowTitle(title, subtitle, duration, fadeIn, fadeOut);

        Reply(ctx, $"Title sent to {Coloured(players.Count())} player{(players.Count() == 1 ? "" : "s")}.");
        return players.Count();
    }
}