using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Exceptions;

namespace MoreCommands.ArgumentTypes;

public class RangeArgumentType : ArgumentType<FloatRange>
{
    public override FloatRange Parse(IStringReader reader) => FloatRange.Parse(reader);
}

public struct FloatRange
{
    public static FloatRange Any => Between();
    private static readonly SimpleCommandExceptionType Empty = new(new LiteralMessage("No characters left to read"));
    public float? From;
    public float? To;

    public static FloatRange Between(float? from = null, float? to = null) => new()
    {
        From = from,
        To = to
    };

    public static FloatRange Parse(IStringReader reader)
    {
        if (!reader.CanRead()) {
            throw Empty.CreateWithContext(reader);
        }
        
        int i = reader.Cursor;
        try {
            float? max;
            float? min = FromStringReader(reader);
            
            if (reader.CanRead(2) && reader.Peek() == '.' && reader.Peek(1) == '.') {
                reader.Skip();
                reader.Skip();
                
                max = FromStringReader(reader);
                if (min == null && max == null) 
                    throw Empty.CreateWithContext(reader);
            }
            else max = min;
            
            if (min == null && max == null) 
                throw Empty.CreateWithContext(reader);
            
            return new FloatRange
            {
                From = min,
                To = max
            };
        }
        catch (CommandSyntaxException number) {
            reader.Cursor = i;
            throw new CommandSyntaxException(number.Type, number.RawMessage(), number.Input, i);
        }
    }
    
    private static float? FromStringReader(IStringReader reader) {
        int cursor = reader.Cursor;
        
        while (reader.CanRead() && IsNextCharValid(reader)) {
            reader.Skip();
        }
        
        string s = reader.String[cursor..reader.Cursor];
        if (s.Length == 0) return null;

        if (float.TryParse(s, out float f))
            return f;

        throw CommandSyntaxException.BuiltInExceptions.ReaderInvalidFloat().CreateWithContext(reader, s);
    }
    
    private static bool IsNextCharValid(IImmutableStringReader reader) {
        char c = reader.Peek();
        switch (c)
        {
            case >= '0' and <= '9':
            case '-':
                return true;
            case '.':
                return !reader.CanRead(2) || reader.Peek(1) != '.';
            default:
                return false;
        }
    }

    public override string ToString() => $"{{{From?.ToString() ?? ""}..{To?.ToString() ?? ""}}}";
}