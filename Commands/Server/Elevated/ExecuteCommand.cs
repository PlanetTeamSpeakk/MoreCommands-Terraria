using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Tree;
using Microsoft.Xna.Framework;
using MoreCommands.ArgumentTypes;
using MoreCommands.ArgumentTypes.Entities;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class ExecuteCommand : Command
{
    private delegate bool Condition(CommandContext<CommandSource> source);
    private delegate int ExistsCondition(CommandContext<CommandSource> source);
    private static readonly Dynamic2CommandExceptionType BlocksTooBigException = new((maxCount, count) => new LiteralMessage("The given selection is too big. " +
                                                                                                                             $"(Maximum allowed: {maxCount}, selection: {count})"));
    private static readonly SimpleCommandExceptionType ConditionalFailException = new(new LiteralMessage("The condition has failed."));
    private static readonly DynamicCommandExceptionType ConditionalFailCountException = new(count => new LiteralMessage($"{count} conditions have failed."));
    public override CommandType Type => CommandType.World;
    public override string Description => "Execute commands as other people.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        LiteralCommandNode<CommandSource> execute = dispatcher.Register(RootLiteralReq("execute"));
        dispatcher.Register(RootLiteralReq("execute")
            .Then(Literal("run")
                .Redirect(dispatcher.GetRoot()))
            .Then(AddConditionArguments(execute, Literal("if"), true))
            .Then(AddConditionArguments(execute, Literal("unless"), false))
            .Then(Literal("privileged")
                .Redirect(execute, ctx => ctx.Source.WithOp(true)))
            .Then(Literal("as")
                .Then(Argument("player", EntityArgumentType.Player)
                    .Fork(execute, ctx => 
                        Util.Singleton(ctx.Source.WithCaller(new ServerPlayerCommandCaller(EntityArgumentType.GetPlayer(ctx, "player")))).ToList())))
            .Then(Literal("at")
                .Then(Argument("player", EntityArgumentType.Player)
                    .Fork(execute, ctx => Util.Singleton(ctx.Source.WithPosition(EntityArgumentType.GetPlayer(ctx, "player").Center)).ToList()))
                // .Then(Literal("store")
                //     .Then(addStoreArguments(execute, Literal("result"), true))
                //     .Then(addStoreArguments(execute, Literal("success"), false)))
                .Then(Literal("positioned")
                    .Then(Argument("pos", PositionArgumentType.Pos)
                        .Redirect(execute, ctx => ctx.Source.WithPosition(PositionArgumentType.GetPosition(ctx, "pos"))))
                    .Then(Literal("as")
                        .Then(Argument("player", EntityArgumentType.Player)
                            .Fork(execute, ctx => Util.Singleton(ctx.Source.WithPosition(EntityArgumentType.GetPlayer(ctx, "player").Center)).ToList()))))));
    }
    
    private static LiteralArgumentBuilder<CommandSource> AddConditionArguments(CommandNode<CommandSource> root, LiteralArgumentBuilder<CommandSource> argumentBuilder, bool positive)
    {
        argumentBuilder
            .Then(Literal("tile")
                .Then(Argument("pos", PositionArgumentType.TilePos)
                    .Then(AddConditionLogic(root, Argument("tile", IdArgumentType.Tile), positive, 
                        ctx => Framing.GetTileSafely(PositionArgumentType.GetPositionVec(ctx, "pos"))?.type == ctx.GetArgument<int>("tile")))))
            // .Then(Literal("score") // TODO add scoreboard
            //     .Then(Argument("target", ScoreHolderArgumentType.scoreHolder())
            //         .Suggests(ScoreHolderArgumentType.SUGGESTION_PROVIDER)
            //         .Then(Argument("targetObjective", ScoreboardObjectiveArgumentType.scoreboardObjective())
            //             .Then(Literal("=")
            //                 .Then(Argument("source", ScoreHolderArgumentType.scoreHolder())
            //                     .Suggests(ScoreHolderArgumentType.SUGGESTION_PROVIDER)
            //                     .Then(addConditionLogic(root, Argument("sourceObjective", ScoreboardObjectiveArgumentType.scoreboardObjective()), positive, 
            //                         ctx => testScoreCondition(ctx, Integer::@equals)))))
            //             .Then(Literal("<")
            //                 .Then(Argument("source", ScoreHolderArgumentType.scoreHolder())
            //                     .Suggests(ScoreHolderArgumentType.SUGGESTION_PROVIDER)
            //                     .Then(addConditionLogic(root, Argument("sourceObjective", ScoreboardObjectiveArgumentType.scoreboardObjective()), positive, 
            //                         ctx => testScoreCondition(ctx, (a, b) => a < b)))))
            //             .Then(Literal("<=")
            //                 .Then(Argument("source", ScoreHolderArgumentType.scoreHolder())
            //                     .Suggests(ScoreHolderArgumentType.SUGGESTION_PROVIDER)
            //                     .Then(addConditionLogic(root, Argument("sourceObjective", ScoreboardObjectiveArgumentType.scoreboardObjective()), positive, 
            //                         ctx => testScoreCondition(ctx, (a, b) => a <= b)))))
            //             .Then(Literal(">")
            //                 .Then(Argument("source", ScoreHolderArgumentType.scoreHolder())
            //                     .Suggests(ScoreHolderArgumentType.SUGGESTION_PROVIDER)
            //                     .Then(addConditionLogic(root, Argument("sourceObjective", ScoreboardObjectiveArgumentType.scoreboardObjective()), positive, 
            //                         ctx => testScoreCondition(ctx, (a, b) => a > b)))))
            //             .Then(Literal(">=")
            //                 .Then(Argument("source", ScoreHolderArgumentType.scoreHolder())
            //                     .Suggests(ScoreHolderArgumentType.SUGGESTION_PROVIDER)
            //                     .Then(addConditionLogic(root, Argument("sourceObjective", ScoreboardObjectiveArgumentType.scoreboardObjective()), positive, 
            //                         ctx => testScoreCondition(ctx, (a, b) => a >= b)))))).Then(Literal("matches")
            //             .Then(addConditionLogic(root, Argument("range", NumberRangeArgumentType.intRange()), positive, 
            //                 ctx => testScoreMatch(ctx, NumberRangeArgumentType.IntRangeArgumentType.getRangeArgument(ctx, "range")))))))
            .Then(Literal("tiles")
                .Then(Argument("start", PositionArgumentType.TilePos)
                    .Then(Argument("end", PositionArgumentType.TilePos)
                        .Then(Argument("destination", PositionArgumentType.TilePos)
                            .Then(AddBlocksConditionLogic(root, Literal("all"), positive, false))
                            .Then(AddBlocksConditionLogic(root, Literal("masked"), positive, true))))))
            .Then(Literal("npcs")
                .Then(Argument("npcs", EntityArgumentType.Entities)
                    .Fork(root, ctx => GetSourceOrEmptyForConditionFork(ctx, positive, EntityArgumentType.GetEntities(ctx, "npcs").Any()))
                    .Executes(GetExistsConditionExecute(positive, ctx => EntityArgumentType.GetEntities(ctx, "npcs").Count))));
        
        return argumentBuilder;
    }
    
    private static ArgumentBuilder<CommandSource, TBuilder, TNode> AddConditionLogic<TBuilder, TNode>(CommandNode<CommandSource> root, ArgumentBuilder<CommandSource, TBuilder, TNode> builder,
        bool positive, Condition condition) where TBuilder : ArgumentBuilder<CommandSource, TBuilder, TNode> where TNode: CommandNode<CommandSource>
    {
        return builder.Fork(root, ctx => GetSourceOrEmptyForConditionFork(ctx, positive, condition(ctx)))
            .Executes(ctx =>
            {
                if (positive != condition(ctx)) throw ConditionalFailException.Create();
                
                Reply(ctx, "The condition has passed.");
                return 1;
            });
    }
    
    private static ArgumentBuilder<CommandSource, TBuilder, TNode> AddBlocksConditionLogic<TBuilder, TNode>(CommandNode<CommandSource> root, ArgumentBuilder<CommandSource, TBuilder, TNode> builder,
        bool positive, bool masked) where TBuilder : ArgumentBuilder<CommandSource, TBuilder, TNode> where TNode : CommandNode<CommandSource> =>
        builder.Fork(root, ctx => GetSourceOrEmptyForConditionFork(ctx, positive, TestBlocksCondition(ctx, masked) is not null))
            .Executes(positive ? ctx => ExecutePositiveBlockCondition(ctx, masked) : ctx => ExecuteNegativeBlockCondition(ctx, masked));
    
    private static IList<CommandSource> GetSourceOrEmptyForConditionFork(CommandContext<CommandSource> context, bool positive, bool value) =>
        (value == positive ? Enumerable.Repeat(context.Source, 1) : Array.Empty<CommandSource>()).ToImmutableList();
    
    private static int? TestBlocksCondition(CommandContext<CommandSource> ctx, bool masked) {
        return TestBlocksCondition(PositionArgumentType.GetPositionVec(ctx, "start"), PositionArgumentType.GetPositionVec(ctx, "end"), 
            PositionArgumentType.GetPositionVec(ctx, "destination"), masked);
    }

    private static int? TestBlocksCondition(Vector2 start, Vector2 end, Vector2 destination, bool masked) {
        Rectangle rect1 = Util.CreateRectangle(start, end);
        Rectangle rect2 = Util.CreateRectangle(destination, destination + rect1.Size());
        Vector2 pos = new(rect2.X - rect1.X, rect2.Y - rect1.Y);
        
        int i = rect1.Width * rect1.Height;
        if (i > 32768) throw BlocksTooBigException.Create(32768, i);
        
        int j = 0;
        for (int y = rect1.Y; y <= rect1.Y + rect1.Height; ++y)
        for (int x = rect1.X; x <= rect1.X + rect1.Width; ++x) {
            Vector2 pos2 = new(x, y);
            Vector2 pos3 = pos2 + pos;
            
            if (masked && !Main.tile[x, y].IsActive) continue;
            if (Main.tile[x, y].type != Main.tile[(int) pos3.X, (int) pos3.Y].type)
                return null;
            
            ++j;
        }
        
        return j;
    }
    
    private static Command<CommandSource> GetExistsConditionExecute(bool positive, ExistsCondition condition) {
        if (positive)
            return ctx => {
                int i = condition(ctx);
                if (i <= 0) throw ConditionalFailException.Create();
                
                Reply(ctx, $"{Coloured(i)} conditions passed.");
                return i;
            };
        
        return ctx => {
            int i = condition(ctx);
            if (i != 0) throw ConditionalFailCountException.Create(i);
            
            Reply(ctx, "The condition has passed.");
            return 1;
        };
    }
    
    private static int ExecutePositiveBlockCondition(CommandContext<CommandSource> ctx, bool masked) {
        int? blocks = TestBlocksCondition(ctx, masked);
        if (blocks is null) throw ConditionalFailException.Create();
        
        Reply(ctx, $"{blocks.Value} passed the condition.");
        return blocks.Value;
    }

    private static int ExecuteNegativeBlockCondition(CommandContext<CommandSource> ctx, bool masked) {
        int? blocks = TestBlocksCondition(ctx, masked);
        if (blocks is not null) 
            throw ConditionalFailCountException.Create(blocks.Value);
        
        Reply(ctx, "The condition has passed.");
        return 1;
    }
}