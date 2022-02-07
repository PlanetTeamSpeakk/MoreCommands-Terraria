using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Suggestion;
using Terraria;

namespace MoreCommands.ArgumentTypes;

public class PlayerArgumentType : ArgumentType<Player>
{
    private static readonly SimpleCommandExceptionType NotFound = new(new LiteralMessage("A player by the given name could not be found."));
    private static readonly StringArgumentType Parent = Arguments.String();

    private PlayerArgumentType() {}

    public static PlayerArgumentType Player => new();

    public override Player Parse(IStringReader reader)
    {
        string query = Parent.Parse(reader);
        Player player = Main.player.FirstOrDefault(p => p.name == query);

        if (player is null)
            throw NotFound.CreateWithContext(reader);

        return player;
    }

    public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
    {
        string input = builder.Remaining.ToLower();

        if (input.StartsWith('"') && input.EndsWith('"'))
            input = input[1..^1];
        else if (input.Contains(' ')) input = input[..input.IndexOf(' ')];

        foreach ((string suggestion, Player player) in Main.player
                     .Where(p => p.name.ToLower().StartsWith(input))
                     .Select(p => (string.Format(p.name.Contains(' ') ? "\"{0}\"" : "{0}", p.name), p)))
            builder.Suggest(suggestion, new LiteralMessage(player.name));

        return builder.BuildFuture();
    }

    public override IEnumerable<string> Examples => new []{"PlanetTeamSpeak", "\"Mr. Nobody\""};
}