using System.Collections.Generic;
using Brigadier.NET;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Tree;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace MoreCommands.Misc;

public class ModCommandWrapper : ModCommand
{
    public override string Command { get; }
    public override CommandType Type => _command.Type;
    public override string Description => _command.Description;
    public override string Usage { get; }
    public bool Console => _command.Console;
    private readonly CommandDispatcher<CommandSource> _dispatcher;
    private readonly Command _command;

    public ModCommandWrapper(CommandDispatcher<CommandSource> dispatcher, string name, Command command)
    {
        _dispatcher = dispatcher;
        Command = name;
        _command = command;
        IDictionary<CommandNode<CommandSource>, string> usages = dispatcher.GetSmartUsage((LiteralCommandNode<CommandSource>) dispatcher.GetRoot().GetChild(name), new CommandSource(new DummyCommandCaller()));
        Usage = usages.Count == 0 ? "/" + name : $"/{name} " + string.Join($" OR /{name} ", usages.Values);
    }

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        try
        {
            _dispatcher.Execute(input.StartsWith("/") ? input[1..] : input, new CommandSource(caller));
        }
        catch (CommandSyntaxException e)
        {
            caller.Reply(e.Message, Color.Red);
        }
    }
}