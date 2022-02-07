using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Suggestion;
using Microsoft.Xna.Framework;
using MoreCommands.Extensions;
using Terraria;

namespace MoreCommands.ArgumentTypes.Entities;

public class EntitySelectorReader
{
    private static readonly SimpleCommandExceptionType InvalidEntityException = new(new LiteralMessage("Invalid name"));
    private static readonly DynamicCommandExceptionType UnknownSelectorException = new(selectorType => new LiteralMessage("Unknown selector type " + selectorType));
    private static readonly SimpleCommandExceptionType NotAllowedException = new(new LiteralMessage("Selector not allowed"));
    private static readonly SimpleCommandExceptionType MissingException = new(new LiteralMessage("Missing selector type"));
    private static readonly SimpleCommandExceptionType UnterminatedException = new(new LiteralMessage("Expected end of options"));
    private static readonly DynamicCommandExceptionType ValuelessException = new(option => new LiteralMessage("Expected value for option " + option));
    public static readonly Action<Vector2, List<Entity>> Arbitrary = (_, _) => {};
    public static readonly Action<Vector2, List<Entity>> Nearest = (pos, entities) => entities.Sort((first, second) => first.position.DistanceSQ(pos).CompareTo(second.position.DistanceSQ(pos)));
    public static readonly Action<Vector2, List<Entity>> Furthest = (pos, entities) => entities.Sort((first, second) => second.position.DistanceSQ(pos).CompareTo(first.position.DistanceSQ(pos)));
    public static readonly Action<Vector2, List<Entity>> Random = (_, entities) => entities.Shuffle();
    private static readonly Func<SuggestionsBuilder, Action<SuggestionsBuilder>, Task<Suggestions>> DefaultSuggestionProvider = (builder, _) => builder.BuildFuture();
    private readonly bool _atAllowed;
    
    public IStringReader Reader { get; }
    public bool IncludesNonPlayers { get; set; }
    public int Limit { get; set; }
    public int Type { get; set; }
    public FloatRange Distance { get; set; } = FloatRange.Any;
    public float? X { get; set; }
    public float? Y { get; set; }
    public float? Dx { get; set; }
    public float? Dy { get; set; }
    public Predicate<Entity> Predicate
    {
        get => _predicate;
        set
        {
            Predicate<Entity> old = _predicate;
            _predicate = entity => old(entity) && value(entity);
        }
    }
    private Func<SuggestionsBuilder, Action<SuggestionsBuilder>, Task<Suggestions>> SuggestionProvider { get; set; } = DefaultSuggestionProvider;
    public bool SelectsName { get; set; }
    public bool ExcludesName { get; set; }
    public bool HasLimit { get; set; }
    public bool SelectsType { get; set; }
    public bool ExcludesType { get; set; }
    public bool HasSorter { get; set; }
    public bool SelectsTeam { get; set; }
    public bool ExcludesTeam { get; set; }
    public bool SelectsScores { get; set; }
    public bool UsesAt { get; set; }
    public Action<Vector2, List<Entity>> Sorter { get; set; } = Arbitrary;
    private Predicate<Entity> _predicate = _ => true;
    private bool _senderOnly;
    private string _playerName;
    private int _startCursor;

    public EntitySelectorReader(IStringReader reader, bool atAllowed = true)
    {
        Reader = reader;
        _atAllowed = atAllowed;
    }

    public EntitySelector Build()
    {
        Func<Vector2, Vector2> positionOffset = X == null && Y == null ? pos => pos : pos => new Vector2(X ?? pos.X, Y ?? pos.Y);
        return new EntitySelector(Limit, IncludesNonPlayers, _predicate, Distance, positionOffset, Sorter, _senderOnly, _playerName, UsesAt);
    }
    
    private void ReadAtVariable()
    {
        UsesAt = true;
        SuggestionProvider = SuggestSelectorRest;
        if (!Reader.CanRead())
        {
            throw MissingException.CreateWithContext(Reader);
        }
        int i = Reader.Cursor;
        char c = Reader.Read();

        switch (c)
        {
            case 'p':
                Limit = 1;
                IncludesNonPlayers = false;
                Sorter = Nearest;
                break;
            case 'a':
                Limit = int.MaxValue;
                IncludesNonPlayers = false;
                Sorter = Arbitrary;
                break;
            case 'r':
                Limit = 1;
                IncludesNonPlayers = false;
                Sorter = Random;
                break;
            case 's':
                Limit = 1;
                IncludesNonPlayers = true;
                _senderOnly = true;
                break;
            case 'e':
                Limit = int.MaxValue;
                IncludesNonPlayers = true;
                Sorter = Arbitrary;
                _predicate = entity => entity is Player {statLife: > 0} or NPC {life: > 0};
                break;
            default:
                Reader.Cursor = i;
                throw UnknownSelectorException.CreateWithContext(Reader, "@" + c);
        }

        SuggestionProvider = SuggestOpen;
        if (!Reader.CanRead() || Reader.Peek() != '[') return;

        Reader.Skip();
        SuggestionProvider = SuggestOptionOrEnd;
        ReadArguments();
    }

    private void ReadRegular()
    {
        if (Reader.CanRead())
            SuggestionProvider = SuggestNormal;

        int i = Reader.Cursor;
        string s = Reader.ReadString();

        if (s.Length is 0 or > 16)
        {
            Reader.Cursor = i;
            throw InvalidEntityException.CreateWithContext(Reader);
        }

        IncludesNonPlayers = false;
        _playerName = s;
        Limit = 1;
    }

    private void ReadArguments()
    {
        SuggestionProvider = SuggestOption;
        Reader.SkipWhitespace();

        while (Reader.CanRead() && Reader.Peek() != ']')
        {
            Reader.SkipWhitespace();
            int i = Reader.Cursor;
            string s = Reader.ReadString();

            EntitySelectorOptions.SelectorHandler selectorHandler = EntitySelectorOptions.GetHandler(this, s, i);
            Reader.SkipWhitespace();

            if (!Reader.CanRead() || Reader.Peek() != '=')
            {
                Reader.Cursor = i;
                throw ValuelessException.CreateWithContext(Reader, s);
            }

            Reader.Skip();
            Reader.SkipWhitespace();

            SuggestionProvider = DefaultSuggestionProvider;
            selectorHandler(this);

            Reader.SkipWhitespace();
            SuggestionProvider = SuggestEndNext;
            if (!Reader.CanRead()) continue;

            if (Reader.Peek() == ',')
            {
                Reader.Skip();
                SuggestionProvider = SuggestOption;
                continue;
            }

            if (Reader.Peek() == ']') break;
            throw UnterminatedException.CreateWithContext(Reader);
        }
        if (!Reader.CanRead())
            throw UnterminatedException.CreateWithContext(Reader);

        Reader.Skip();
        SuggestionProvider = DefaultSuggestionProvider;
    }

    public bool ReadNegationCharacter()
    {
        Reader.SkipWhitespace();
        if (!Reader.CanRead() || Reader.Peek() != '!') return false;

        Reader.Skip();
        Reader.SkipWhitespace();
        return true;

    }

    public bool ReadTagCharacter()
    {
        Reader.SkipWhitespace();
        if (!Reader.CanRead() || Reader.Peek() != '#') return false;

        Reader.Skip();
        Reader.SkipWhitespace();
        return true;
    }

    public EntitySelector Read()
    {
        _startCursor = Reader.Cursor;
        SuggestionProvider = SuggestSelector;
        if (Reader.CanRead() && Reader.Peek() == '@')
        {
            if (!_atAllowed)
                throw NotAllowedException.CreateWithContext(Reader);

            Reader.Skip();
            ReadAtVariable();
        }
        else ReadRegular();

        return Build();
    }

    private static void SuggestSelector(SuggestionsBuilder builder)
    {
        builder.Suggest("@p", new LiteralMessage("Nearest player"));
        builder.Suggest("@a", new LiteralMessage("All players"));
        builder.Suggest("@r", new LiteralMessage("Random player"));
        builder.Suggest("@s", new LiteralMessage("Sender"));
        builder.Suggest("@e", new LiteralMessage("All entities"));
    }

    private Task<Suggestions> SuggestSelector(SuggestionsBuilder builder, Action<SuggestionsBuilder> consumer)
    {
        consumer(builder);
        if (_atAllowed)
            SuggestSelector(builder);
        return builder.BuildFuture();
    }

    private Task<Suggestions> SuggestNormal(SuggestionsBuilder builder, Action<SuggestionsBuilder> consumer)
    {
        SuggestionsBuilder suggestionsBuilder = builder.CreateOffset(_startCursor);

        consumer(suggestionsBuilder);
        return builder.Add(suggestionsBuilder).BuildFuture();
    }

    private static Task<Suggestions> SuggestSelectorRest(SuggestionsBuilder builder, Action<SuggestionsBuilder> consumer)
    {
        SuggestionsBuilder suggestionsBuilder = builder.CreateOffset(builder.Start - 1);
        SuggestSelector(suggestionsBuilder);
        builder.Add(suggestionsBuilder);
        return builder.BuildFuture();
    }

    private static Task<Suggestions> SuggestOpen(SuggestionsBuilder builder, Action<SuggestionsBuilder> consumer)
    {
        builder.Suggest("[");
        return builder.BuildFuture();
    }

    private Task<Suggestions> SuggestOptionOrEnd(SuggestionsBuilder builder, Action<SuggestionsBuilder> consumer)
    {
        builder.Suggest("]");
        EntitySelectorOptions.SuggestOptions(this, builder);
        return builder.BuildFuture();
    }

    private Task<Suggestions> SuggestOption(SuggestionsBuilder builder, Action<SuggestionsBuilder> consumer)
    {
        EntitySelectorOptions.SuggestOptions(this, builder);
        return builder.BuildFuture();
    }

    private static Task<Suggestions> SuggestEndNext(SuggestionsBuilder builder, Action<SuggestionsBuilder> consumer)
    {
        builder.Suggest(",");
        builder.Suggest("]");
        return builder.BuildFuture();
    }
    
    public bool isSenderOnly()
    {
        return _senderOnly;
    }

    public void SetSuggestionProvider(Func<SuggestionsBuilder, Action<SuggestionsBuilder>, Task<Suggestions>> suggestionProvider)
    {
        SuggestionProvider = suggestionProvider;
    }

    public Task<Suggestions> ListSuggestions(SuggestionsBuilder builder, Action<SuggestionsBuilder> consumer)
    {
        return SuggestionProvider(builder.CreateOffset(Reader.Cursor), consumer);
    }
}