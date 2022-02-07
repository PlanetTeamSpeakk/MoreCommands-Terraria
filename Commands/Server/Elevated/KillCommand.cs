using System.Collections.Generic;
using Brigadier.NET;
using MoreCommands.ArgumentTypes;
using MoreCommands.ArgumentTypes.Entities;
using MoreCommands.Extensions;
using MoreCommands.Misc;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class KillCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Kill anything and everything you wish.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("kill")
            .Executes(ctx => dispatcher.GetRoot().GetChild("suicide").Command(ctx))
            .Then(Argument("entities", EntityArgumentType.Entities)
                .Executes(ctx =>
                {
                    List<Entity> entities = EntityArgumentType.GetEntities(ctx, "entities");
                    entities.ForEach(entity => entity.Kill());
                    
                    Reply(ctx, $"Successfully killed {Coloured(entities.Count)} entit{(entities.Count == 1 ? "y" : "ies")}.");
                    return entities.Count;
                })));
    }
}