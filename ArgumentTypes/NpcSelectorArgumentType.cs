using System;
using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Exceptions;
using Terraria;

namespace MoreCommands.ArgumentTypes;

public class NpcSelectorArgumentType : ArgumentType<IEnumerable<NPC>>
{
    private static readonly SimpleCommandExceptionType Invalid = new(new LiteralMessage("Invalid character found"));
    
    private NpcSelectorArgumentType() {}

    public static NpcSelectorArgumentType NpcSelector => new();
    
    public override IEnumerable<NPC> Parse(IStringReader reader)
    {
        int cursor = reader.Cursor;
        IDictionary<string, object> entries = new Dictionary<string, object>();

        string key = "";

        bool readingVal = false;
        while (reader.Cursor < reader.String.Length && reader.Peek() != ' ')
        {
            if (reader.Peek() == '=')
            {
                if (!readingVal)
                {
                    key = reader.String[cursor..reader.Cursor];

                    cursor = reader.Cursor + 1;
                    readingVal = true;
                }
            }
            else if (reader.Peek() == ',' || reader.Cursor == reader.String.Length - 1)
            {
                string value = reader.String[cursor..(reader.Cursor == reader.String.Length - 1 ? reader.Cursor + 1 : reader.Cursor)];
                cursor = reader.Cursor + 1;
                
                entries[key] = value.All(char.IsDigit) ? int.Parse(value) : value.All(ch => ch == '.' || char.IsDigit(ch)) ? float.Parse(value) : value;

                key = "";
                readingVal = false;
            }

            reader.Skip();
        }
        
        MoreCommands.Log.Debug("Found entries: " + string.Join(", ", entries));

        return Array.Empty<NPC>();
    }
}