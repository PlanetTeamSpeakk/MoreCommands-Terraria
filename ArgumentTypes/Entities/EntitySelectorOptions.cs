using System;
using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Suggestion;
using MoreCommands.Utils;
using Terraria;
using Terraria.Enums;
using Terraria.ID;

namespace MoreCommands.ArgumentTypes.Entities;

public static class EntitySelectorOptions
{
    private static readonly IDictionary<string, SelectorOption> Options = new Dictionary<string, SelectorOption>();
    private static readonly DynamicCommandExceptionType UnknownOptionException = new(option => new LiteralMessage($"Unknown option '{option}"));
    private static readonly DynamicCommandExceptionType InapplicableOptionException = new(option => new LiteralMessage($"Option '{option}' isn't applicable here"));
    private static readonly SimpleCommandExceptionType NegativeDistanceException = new(new LiteralMessage("Distance cannot be negative"));
    private static readonly SimpleCommandExceptionType TooSmallLevelException = new(new LiteralMessage("Limit must be at least 1"));
    private static readonly DynamicCommandExceptionType IrreversibleSortException = new(sortType => new LiteralMessage($"Invalid or unknown sort type '{sortType}'"));
    private static readonly SimpleCommandExceptionType InvalidTypeException = new(new LiteralMessage("Unknown type"));
    private static readonly SimpleCommandExceptionType InvalidTagGroupException = new(new LiteralMessage("Unknown tag group"));
    private static readonly IDictionary<string, List<int>> TypeTagGroups = new Dictionary<string, List<int>>();

    public delegate void SelectorHandler(EntitySelectorReader reader);

    private static void PutOption(string id, SelectorHandler handler, Predicate<EntitySelectorReader> condition, string description) =>
        Options[id] = new SelectorOption(handler, condition, description);

    internal static void Register()
    {
        if (Options.Count > 0)
            return;

        PutOption("name", reader =>
        {
            int cursor = reader.Reader.Cursor;
            bool negated = reader.ReadNegationCharacter();
            string s = reader.Reader.ReadString();

            if (reader.ExcludesName && !negated)
            {
                reader.Reader.Cursor = cursor;
                throw InapplicableOptionException.CreateWithContext(reader.Reader, "name");
            }

            if (negated) reader.ExcludesName = true;
            else reader.SelectsName = true;

            reader.Predicate = entity => entity is Player p && p.name == s != negated;
        }, reader => !reader.SelectsName, "Player name");

        PutOption("distance", reader =>
        {
            int cursor = reader.Reader.Cursor;
            FloatRange floatRange = FloatRange.Parse(reader.Reader);

            if (floatRange.From is not null && floatRange.From < 0.0 || floatRange.To is not null && floatRange.To < 0.0)
            {
                reader.Reader.Cursor = cursor;
                throw NegativeDistanceException.CreateWithContext(reader.Reader);
            }

            reader.Distance = floatRange;
        }, reader => reader.Distance.From is null && reader.Distance.To is null, "Distance to entity");

        PutOption("x", reader => reader.X = reader.Reader.ReadFloat(), reader => reader.X is null, "X position");
        PutOption("y", reader => reader.Y = reader.Reader.ReadFloat(), reader => reader.Y is null, "Y position");
        PutOption("dx", reader => reader.Dx = reader.Reader.ReadFloat(), reader => reader.Dx is null, "Between x and x + dx");
        PutOption("dy", reader => reader.Dy = reader.Reader.ReadFloat(), reader => reader.Dy is null, "Between y and y + dy");
        
        PutOption("limit", reader =>
        {
            int cursor = reader.Reader.Cursor;
            int limit = reader.Reader.ReadInt();

            if (limit < 1)
            {
                reader.Reader.Cursor = cursor;
                throw TooSmallLevelException.CreateWithContext(reader.Reader);
            }
            reader.Limit = limit;
            reader.HasLimit = true;
        }, reader => !reader.isSenderOnly() && !reader.HasLimit, "Maximum number of entities to return");

        PutOption("sort", reader =>
        {
            int i = reader.Reader.Cursor;
            string s = reader.Reader.ReadUnquotedString();
            
            reader.SetSuggestionProvider((builder, _) =>
            {
                string rem = builder.Remaining.ToLower();

                foreach (string option in new[] {"nearest", "furthest", "random", "arbitrary"})
                    if (option.StartsWith(rem))
                        builder.Suggest(option);

                return builder.BuildFuture();
            });
            
            reader.Sorter = s switch
            {
                "nearest" => EntitySelectorReader.Nearest,
                "furthest" => EntitySelectorReader.Furthest,
                "random" => EntitySelectorReader.Random,
                "arbitrary" => EntitySelectorReader.Arbitrary,
                _ => delegate
                {
                    reader.Reader.Cursor = i;
                    throw IrreversibleSortException.CreateWithContext(reader.Reader, s);
                }
            };

            reader.HasSorter = true;
        }, reader => !reader.isSenderOnly() && !reader.HasSorter, "Sort the entities");

        PutOption("team", reader =>
        {
            int cursor = reader.Reader.Cursor;
            bool negated = reader.ReadNegationCharacter();
            string s = reader.Reader.ReadUnquotedString();

            if (reader.ExcludesTeam && !negated)
            {
                reader.Reader.Cursor = cursor;
                throw InapplicableOptionException.CreateWithContext(reader.Reader, "team");
            }
            
            reader.Predicate = entity =>
            {
                if (entity is not Player player || !Enum.TryParse(s, true, out Team team))
                    return false;

                return player.team == (int) team;
            };

            if (negated) reader.ExcludesTeam = true;
            else reader.SelectsTeam = true;
        }, reader => !reader.SelectsTeam, "Entities on team");

        PutOption("type", reader =>
        {
            reader.SetSuggestionProvider((builder, _) =>
            {
                SuggestionsBuilder builder0 = new(builder.Input, builder.Start + (builder.Start < builder.Input.Length && builder.Input[builder.Start] == '!' ? 1 : 0));
                
                IdArgumentType.Entity.ListSuggestions(builder0);
                Suggestions suggestions = builder0.Build();
                
                suggestions.List.ForEach(suggestion => builder.Suggest("!" + suggestion.Text, new LiteralMessage("!" + suggestion.Tooltip.String)));
                suggestions.List.ForEach(suggestion => builder.Suggest(suggestion.Text, suggestion.Tooltip));

                string rem = builder.Remaining.ToLower();
                foreach (string key in TypeTagGroups.Keys)
                    if (('#' + key).StartsWith(rem) || ("!#" + key).StartsWith(rem))
                    {
                        builder.Suggest("!#" + key);
                        builder.Suggest("#" + key);
                    }
                
                return builder.BuildFuture();
            });

            int cursor = reader.Reader.Cursor;
            bool negated = reader.ReadNegationCharacter();
            
            if (reader.ExcludesType && !negated)
            {
                reader.Reader.Cursor = cursor;
                throw InapplicableOptionException.CreateWithContext(reader.Reader, "type");
            }
            
            if (negated)
                reader.ExcludesType = true;

            if (reader.ReadTagCharacter())
            {
                string group = reader.Reader.ReadUnquotedString().ToLower();
                if (!TypeTagGroups.ContainsKey(group))
                {
                    reader.Reader.Cursor = cursor;
                    throw InvalidTagGroupException.Create();
                }

                reader.Predicate = entity => entity is NPC npc && TypeTagGroups[@group].Contains(npc.type) != negated ||
                                             entity is Player && TypeTagGroups[@group].Contains(int.MaxValue) != negated;
                if (!negated)
                    reader.SelectsType = true;
                
                return;
            }

            int type;
            try
            {
                type = IdArgumentType.Entity.Parse(reader.Reader);
            }
            catch (CommandSyntaxException)
            {
                reader.Reader.Cursor = cursor;
                throw InvalidTypeException.CreateWithContext(reader.Reader);
            }

            if (type == int.MaxValue && !negated) // player
                reader.IncludesNonPlayers = false;
            
            reader.Predicate = entity => entity is NPC npc && npc.type == type != negated;

            if (negated) return;
            reader.Type = type;
            reader.SelectsType = true;
        }, reader => !reader.SelectsType, "Entities of type");
    }

    internal static void RegisterTagGroups()
    {
        TypeTagGroups["skeletons"] = SelectTrueIndices(NPCID.Sets.Skeletons).ToList();
        TypeTagGroups["zombies"] = SelectTrueIndices(NPCID.Sets.Zombies).ToList();
        TypeTagGroups["critters"] = SelectTrueIndices(NPCID.Sets.CountsAsCritter).ToList();
        TypeTagGroups["townpets"] = SelectTrueIndices(NPCID.Sets.IsTownPet).ToList();
        TypeTagGroups["townfolk"] = IdHelper.GetIds(IdType.Npc).Where(type =>
        {
            NPC npc = new();
            npc.SetDefaults(type);

            return npc.isLikeATownNPC; // isTown field (and thus isLikeATownNPC property) is set in SetDefaults.
        }).ToList();
        TypeTagGroups["projectiles"] = SelectTrueIndices(NPCID.Sets.ProjectileNPC).ToList();
        TypeTagGroups["bosses"] = SelectTrueIndices(NPCID.Sets.ShouldBeCountedAsBoss).ToList();
        TypeTagGroups["oldonesarmy"] = SelectTrueIndices(NPCID.Sets.BelongsToInvasionOldOnesArmy).ToList();

        TypeTagGroups["passive"] = TypeTagGroups["townfolk"]
            .Concat(TypeTagGroups["critters"])
            .Concat(TypeTagGroups["townpets"])
            .Concat(SelectTrueIndices(NPCID.Sets.TownCritter))
            .Concat(Util.Singleton(int.MaxValue))
            .Distinct()
            .ToList();
    }

    private static IEnumerable<int> SelectTrueIndices(IEnumerable<bool> enumerable)
    {
        int i = 0;
        return enumerable.Select(b => ((int i, bool b)) (i++, b))
            .Where(tuple => tuple.b)
            .Select(tuple => tuple.i);
    }

    public static SelectorHandler GetHandler(EntitySelectorReader reader, string option, int restoreCursor)
    {
        SelectorOption selectorOption = Options.ContainsKey(option) ? Options[option] : null;
        if (selectorOption is not null)
        {
            if (selectorOption.Condition(reader))
                return selectorOption.Handler;
            
            throw InapplicableOptionException.CreateWithContext(reader.Reader, option);
        }
        
        reader.Reader.Cursor = restoreCursor;
        throw UnknownOptionException.CreateWithContext(reader.Reader, option);
    }

    public static void SuggestOptions(EntitySelectorReader reader, SuggestionsBuilder suggestionBuilder)
    {
        string s = suggestionBuilder.Remaining.ToLower();
        foreach ((string key, SelectorOption option) in Options) {
            if (!option.Condition(reader) || !key.ToLower().StartsWith(s)) continue;
            suggestionBuilder.Suggest(key + "=", new LiteralMessage(option.Description));
        }
    }

    public class SelectorOption
    {
        public readonly SelectorHandler Handler;
        public readonly Predicate<EntitySelectorReader> Condition;
        public readonly string Description;

        public SelectorOption(SelectorHandler handler, Predicate<EntitySelectorReader> condition, string description)
        {
            Handler = handler;
            Condition = condition;
            Description = description;
        }
    }
}