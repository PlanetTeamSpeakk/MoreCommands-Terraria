using Brigadier.NET;
using Brigadier.NET.Exceptions;

namespace MoreCommands.Misc;

public static class MCBuiltInExceptions
{
    public static SimpleCommandExceptionType ReqPlayer { get; } = new (new LiteralMessage("You must either be a player or supply a player to run this command."));
    public static SimpleCommandExceptionType BePlayer { get; } = new (new LiteralMessage("You must be a player or supply a player to run this command."));
}