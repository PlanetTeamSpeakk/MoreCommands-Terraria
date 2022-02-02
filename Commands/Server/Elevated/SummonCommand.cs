using Brigadier.NET;
using MoreCommands.ArgumentTypes;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class SummonCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Summon NPCs";

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("summon")
            .Then(Argument("pos", PositionArgumentType.TilePos)
                .Then(Argument("type", Arguments.Integer(-65))
                    .Executes(ctx =>
                    {
                        (float x, float y) = PositionArgumentType.GetPosition(ctx, "pos");
                        int type = ctx.GetArgument<int>("type");

                        if (type == 0)
                        {
                            Error(ctx, "Cannot spawn NPC of type 0.");
                            return 0;
                        }

                        NPC.NewNPC((int) x, (int) y, type);
                        Reply(ctx, $"An NPC of type {IdHelper.GetName(IdType.Npc, type)} has been summoned.");
                        return type;
                    })))
            .Then(Literal("lookup")
                .Then(Argument("query", Arguments.GreedyString())
                    .Executes(ctx => IdHelper.SearchCommand(ctx, "query", IdType.Npc)))));
    }
}