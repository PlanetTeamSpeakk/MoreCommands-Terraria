using System;
using System.Linq;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Suggestion;
using Microsoft.Xna.Framework.Input;

namespace MoreCommands.ArgumentTypes;

public class KeyArgumentType : ArgumentType<Keys>
{
    private static readonly SimpleCommandExceptionType Invalid = new(new LiteralMessage("The given key is invalid."));
    
    private KeyArgumentType() {}

    public static KeyArgumentType Key => new();
    
    public override Keys Parse(IStringReader reader)
    {
        string s = reader.ReadUnquotedString();

        if (int.TryParse(s, out int keyInt))
        {
            foreach (Keys key in Enum.GetValues<Keys>())
                if ((int) key == keyInt) return key;
            throw Invalid.CreateWithContext(reader);
        }

        if (Enum.TryParse(s, true, out Keys key0))
            return key0;

        throw Invalid.CreateWithContext(reader);
    }

    public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
    {
        string rem = builder.Remaining.ToLower();

        foreach (string s in Enum.GetValues<Keys>().Select(Enum.GetName).Where(key => key.ToLower().StartsWith(rem)))
            builder.Suggest(s);

        return builder.BuildFuture();
    }
}