using Brigadier.NET;

namespace MoreCommands.Extensions;

// ReSharper disable once InconsistentNaming
public static class IStringReaderExtensions
{
    public static char Read(this IStringReader reader)
    {
        char ch = reader.Peek();
        reader.Skip();
        return ch;
    }
}