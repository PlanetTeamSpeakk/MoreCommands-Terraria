using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Exceptions;

namespace MoreCommands.ArgumentTypes;

public class TimeArgumentType : ArgumentType<(int hour, int minute)>
{
    private static readonly DynamicCommandExceptionType InvalidChar = new(o => new LiteralMessage("Invalid character found: " + o));
    private static readonly SimpleCommandExceptionType Invalid = new(new LiteralMessage("The given time was invalid in 24-hour format."));
    
    private TimeArgumentType() {}

    public static TimeArgumentType Time => new();
    
    public override (int hour, int minute) Parse(IStringReader reader)
    {
        int start = reader.Cursor;

        while (reader.CanRead() && reader.Peek() != ' ')
        {
            if (!char.IsDigit(reader.Peek()) && reader.Peek() != ':') throw InvalidChar.CreateWithContext(reader, reader.Peek());
            reader.Skip();
        }

        string s = reader.String[start..reader.Cursor];
        if (s.Length - s.IndexOf(':') - 1 != 2)
            throw Invalid.Create();
        
        int hour = int.Parse(s[..s.IndexOf(':')]);
        int minute = int.Parse(s[(s.IndexOf(':') + 1)..]);

        return (hour, minute);
    }

    public override IEnumerable<string> Examples => new []{"7:30", "14:00"};
}