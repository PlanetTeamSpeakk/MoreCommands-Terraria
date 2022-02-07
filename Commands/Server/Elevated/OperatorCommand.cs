using Brigadier.NET;
using Microsoft.Xna.Framework;
using MoreCommands.ArgumentTypes.Entities;
using MoreCommands.Hooks;
using MoreCommands.Misc;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class OperatorCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Assign or withdraw operator status to or from players.";
    public override bool ServerOnly => true;

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("op").Redirect(dispatcher.Register(RootLiteralReq("operator")
            .Then(Argument("player", EntityArgumentType.Players)
                .Executes(ctx =>
                {
                    Player player = EntityArgumentType.GetPlayer(ctx, "player");

                    SystemHooks systemHooks = ModContent.GetInstance<SystemHooks>();
                    if (MoreCommands.IsOp(player.whoAmI)) systemHooks.Op(player.whoAmI);
                    else systemHooks.Deop(player.whoAmI);
                    
                    Reply(ctx, $"Player {player.name} is {(MoreCommands.IsOp(player.whoAmI) ? Coloured("now", Color.Green) : Coloured("no longer", Color.Red))} an operator.");
                    return MoreCommands.IsOp(player.whoAmI) ? 2 : 1;
                })))));
    }
}