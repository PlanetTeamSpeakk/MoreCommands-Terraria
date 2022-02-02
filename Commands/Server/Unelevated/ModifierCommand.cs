using Brigadier.NET;
using MoreCommands.Misc;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Unelevated;

public class ModifierCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override bool Console => false;
    public override string Description => "Set the modifier of the item you're holding.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("modifier")
            .Executes(ctx =>
            {
                int prefix = ctx.Source.Player.inventory[ctx.Source.Player.selectedItem].prefix;
                Reply(ctx, $"The modifier of the item you're holding is {prefix}.");
                    
                return prefix;
            })
            .Then(Argument("modifier", Arguments.Integer()).Requires(IsOp)
                .Executes(ctx =>
                {
                    int prefix = ctx.GetArgument<int>("modifier");
                    ctx.Source.Player.inventory[ctx.Source.Player.selectedItem].Prefix(prefix);
                    
                    Reply(ctx, $"The modifier of the item you're holding has been set to {prefix}.");
                    return prefix;
                })));
    }
}