using System;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Exceptions;
using MoreCommands.IL;
using Newtonsoft.Json;
using static Newtonsoft.Json.JsonToken;
using Newtonsoft.Json.Linq;
using StringReader = System.IO.StringReader;

namespace MoreCommands.ArgumentTypes;

public class JsonArgumentType : ArgumentType<JToken>
{
    private static readonly SimpleCommandExceptionType Invalid = new(new LiteralMessage("The given JSON is invalid."));
    private static readonly SimpleCommandExceptionType SyntaxError = new(new LiteralMessage("The given JSON could not be parsed."));
    private static readonly Func<JsonTextReader, int> GetCharPos = Dynamics.CreateGetter<JsonTextReader, int>("_charPos");

    public override JToken Parse(IStringReader reader)
    {
        int start = reader.Cursor;
        JsonTextReader j = new(new StringReader(reader.Remaining));

        int objectDepth = 0;
        int arrayDepth = 0;

        while (j.Read()) switch (j.TokenType)
        {
            case StartObject:
                objectDepth++;
                break;
            case EndObject:
                if (objectDepth == 0) UpdateAndThrow(j, reader);
                objectDepth--;
                break;
            case StartArray:
                arrayDepth++;
                break;
            case EndArray:
                if (arrayDepth == 0) UpdateAndThrow(j, reader);
                arrayDepth--;
                break;
            case Undefined:
                UpdateAndThrow(j, reader);
                break;
            case None:
            case StartConstructor:
            case PropertyName:
            case Comment:
            case Raw:
            case Integer:
            case Float:
            case JsonToken.String:
            case JsonToken.Boolean:
            case Null:
            case EndConstructor:
            case Date:
            case Bytes:
            default:
                break;
        }
        
        if (objectDepth != 0 || arrayDepth != 0) UpdateAndThrow(j, reader);

        int end = start + GetCharPos(j);
        JToken result;
        try
        {
            result = JToken.Parse(reader.String[start..end]);
        }
        catch
        {
            throw Invalid.Create();
        }
        
        j.Close();
        reader.Cursor = end;
        return result;
    }

    private static void UpdateAndThrow(JsonTextReader j, IStringReader reader)
    {
        reader.Cursor += GetCharPos(j);
        throw SyntaxError.CreateWithContext(reader);
    }
}