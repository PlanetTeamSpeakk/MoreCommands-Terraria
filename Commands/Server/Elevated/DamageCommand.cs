using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Context;
using MoreCommands.ArgumentTypes.Entities;
using MoreCommands.Extensions;
using MoreCommands.Misc;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class DamageCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Damage entities. {player} in the reason will be replaced with the damaged player's name.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("damage")
            .Then(Argument("entities", EntityArgumentType.Entities)
                .Then(Argument("damage", Arguments.Integer(0))
                    .Executes(ctx => Execute(ctx))
                    .Then(Argument("reason", Arguments.String())
                        .Executes(ctx => Execute(ctx, ctx.GetArgument<string>("reason")))))));
    }

    private static int Execute(CommandContext<CommandSource> ctx, string reason = null)
    {
        int damage = ctx.GetArgument<int>("damage");
        IEnumerable<Entity> entities = EntityArgumentType.GetEntities(ctx, "entities");
        
        foreach (Entity entity in entities)
            entity.Damage(damage, reason);
        
        Reply(ctx, Coloured(entities.Count()) + " entities have been damaged.");
        return entities.Count();
    }
}