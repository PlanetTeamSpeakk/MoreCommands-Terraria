using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Suggestion;
using MoreCommands.Utils;

namespace MoreCommands.ArgumentTypes;

public class IdArgumentType : ArgumentType<int>
{
    private static readonly DynamicCommandExceptionType NotFound = new(o => new LiteralMessage($"No {Enum.GetName((IdType) o)?.ToLower()} was found for the given id."));
    
    private readonly IdType _type;

    private IdArgumentType(IdType type) => _type = type;
    
    public static IdArgumentType Item => new(IdType.Item);
    
    public static IdArgumentType Buff => new(IdType.Buff);
    
    public static IdArgumentType Npc => new(IdType.Npc);
    
    public static IdArgumentType Tile => new(IdType.Tile);
    
    public override int Parse(IStringReader reader)
    {
        string s = reader.ReadUnquotedString().ToLower().Replace('_', ' ');
        if (int.TryParse(s, out int id))
        {
            if (IdHelper.GetIds(_type).Contains(id))
                return id;

            throw NotFound.CreateWithContext(reader, _type);
        }

        if (IdHelper.GetNamesLower(_type).Contains(s))
            return IdHelper.GetId(_type, s);

        throw NotFound.CreateWithContext(reader, _type);
    }

    public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
    {
        string s = builder.Remaining.ToLower();

        if (int.TryParse(s, out int _)) return builder.BuildFuture();

        IDictionary<int, string> suggestions = new Dictionary<int, string>();
        foreach (string id in IdHelper.GetNamesLower(_type).Where(name => name.StartsWith(s)))
            suggestions[IdHelper.GetId(_type, id)] = IdHelper.GetName(_type, IdHelper.GetId(_type, id));

        List<KeyValuePair<int, string>> suggestionsList = suggestions.ToList();
        suggestionsList.Sort((first, second) => string.Compare(first.Value, second.Value, StringComparison.OrdinalIgnoreCase));
        suggestionsList.ForEach(pair => builder.Suggest(pair.Key, new LiteralMessage(pair.Value)));
        
        return builder.BuildFuture();
    }

    public override IEnumerable<string> Examples => new[] { "zenith", "4956" };
}