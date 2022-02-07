using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Suggestion;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;

namespace MoreCommands.ArgumentTypes.Entities;

public class EntityArgumentType : ArgumentType<EntitySelector>
{
    private static readonly SimpleCommandExceptionType TooManyEntitiesException = new(new LiteralMessage("Only one entity is allowed, but the provided selector allows more than one"));
    private static readonly SimpleCommandExceptionType TooManyPlayersException = new(new LiteralMessage("Only one player is allowed, but the provided selector allows more than one"));
    private static readonly SimpleCommandExceptionType PlayerSelectorHasEntitiesException = new(new LiteralMessage("Only players may be affected by this command, but the provided selector includes entities"));
    private readonly bool _singleTarget, _playersOnly;

    private EntityArgumentType(bool singleTarget, bool playersOnly)
    {
        _singleTarget = singleTarget;
        _playersOnly = playersOnly;
    }

    public static EntityArgumentType Entity => new(true, false);
    
    public static EntityArgumentType Entities => new(false, false);
    
    public static EntityArgumentType Player => new(true, true);
    
    public static EntityArgumentType Players => new(false, true);
    
    public override EntitySelector Parse(IStringReader stringReader) {
        EntitySelectorReader entitySelectorReader = new(stringReader);
        EntitySelector entitySelector = entitySelectorReader.Read();
        
        if (entitySelector.Limit > 1 && _singleTarget) {
            stringReader.Cursor = 0;
            throw (_playersOnly ? TooManyPlayersException : TooManyEntitiesException).CreateWithContext(stringReader);
        }

        if (!entitySelector.IncludesNonPlayers || !_playersOnly || entitySelector.SenderOnly) return entitySelector;
        
        stringReader.Cursor = 0;
        throw PlayerSelectorHasEntitiesException.CreateWithContext(stringReader);

    }
    
    public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
    {
        if (context.Source is not CommandSource commandSource) return Suggestions.Empty();
        
        StringReader stringReader = new(builder.Input)
        {
            Cursor = builder.Start
        };
        
        EntitySelectorReader entitySelectorReader = new(stringReader, commandSource.IsOp);
        try 
        {
            entitySelectorReader.Read();
        }
        catch (CommandSyntaxException) {}
        
        return entitySelectorReader.ListSuggestions(builder, builder0 =>
        {
            string remaining = builder0.Remaining.ToLower();
            if (remaining.StartsWith('"')) remaining = remaining[1..];

            foreach (string suggestion in Util.QuoteIfHasSpaces(Main.player
                         .Where(p => p.active)
                         .Select(p => p.name)))
                if (suggestion.ToLower().StartsWith(remaining))
                    if (remaining.Contains(' '))
                        builder0.Suggest(suggestion, new LiteralMessage(suggestion[1..^1]));
                    else builder0.Suggest(suggestion);
        });
    }

    public static Entity GetEntity(CommandContext<CommandSource> ctx, string argName) => ctx.GetArgument<EntitySelector>(argName).GetEntity(ctx.Source);
    
    public static List<Entity> GetEntities(CommandContext<CommandSource> ctx, string argName) => ctx.GetArgument<EntitySelector>(argName).GetEntities(ctx.Source);
    
    public static Player GetPlayer(CommandContext<CommandSource> ctx, string argName) => ctx.GetArgument<EntitySelector>(argName).GetPlayer(ctx.Source);
    
    public static List<Player> GetPlayers(CommandContext<CommandSource> ctx, string argName) => ctx.GetArgument<EntitySelector>(argName).GetPlayers(ctx.Source);
}