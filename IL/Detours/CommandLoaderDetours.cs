using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.IL.Detours;

public class CommandLoaderDetours
{
    private static readonly Func<UsageException, string> GetMsg = Dynamics.CreateGetter<UsageException, string>("msg");
    private static readonly Func<UsageException, Color> GetColor = Dynamics.CreateGetter<UsageException, Color>("color");
    private static bool _informedCommandKeybindUnbound;

    public static bool HandleCommand(string input, CommandCaller caller) => HandleCommand(input, caller, false);
    
    public static bool HandleCommand(string input, CommandCaller caller, bool ignoreWrapped)
    {
        bool onClient = caller.GetType() == typeof(Player).Assembly.GetTypes().FirstOrDefault(t => "ChatCommandCaller" == t.Name);

        // Consume empty commands upon sending.
        // Pressing slash key and pressing enter without a command will not send a command.
        if (input == "/" && onClient) return true;
        
        if (!_informedCommandKeybindUnbound && onClient)
        {
            if (MoreCommands.CommandKeybind.GetAssignedKeys().Count == 0)
                caller.Reply("TIP: to use commands more easily, assign the Chat Command keybind in the Mods section of the Controls settings. (Default is /, aka OemQuestion)", Color.LightGreen);
            
            _informedCommandKeybindUnbound = true;
        }

        string[] source = input.TrimEnd().Split(' ');
        string name = source[0];
        string[] array = source.Skip(1).ToArray();
        
        if (caller.CommandType != CommandType.Console)
        {
            if (name[0] != '/')
                return false;
            name = name[1..];
        }

        if (!GetCommand(caller, name, out ModCommand mc))
            return false;

        try
        {
            mc.Action(caller, input, array);
        }
        catch (Exception ex)
        {
            if (ex is UsageException usageException)
            {
                if (GetMsg(usageException) is not null)
                    caller.Reply(GetMsg(usageException), GetColor(usageException));
                else caller.Reply("Usage: " + mc.Usage, Color.Red);
            }
            else
            {
                caller.Reply($"An unknown error occurred while trying to perform that command ({ex.GetType().Name}).", Color.DarkRed);
                ex.LogDetailed();
            }
        }
        
        return true;
    }

    public static bool GetCommand(CommandCaller caller, string name, out ModCommand mc) => GetCommandInner(caller, name, out mc, false);
    
    public static bool GetCommandInner(CommandCaller caller, string name, out ModCommand mc, bool ignoreWrapped)
    {
        IDictionary<string, List<ModCommand>> commands = Util.GetCommands();
        
        string name1 = null;
        if (name.Contains(':'))
        {
            string[] strArray = name.Split(':');
            name1 = strArray[0];
            name = strArray[1];
        }
        
        mc = null;
        if (!commands.TryGetValue(name, out List<ModCommand> source))
            return false;
        
        List<ModCommand> list = source
            .Where(c => caller.CommandType == CommandType.Console && c is ModCommandWrapper { Console: true } || CommandLoader.Matches(c.Type, caller.CommandType))
            .ToList();

        if (ignoreWrapped) list = list.Where(cmd => cmd is not ModCommandWrapper).ToList();
        if (list.Count == 0)
            return false;
        
        if (name1 is not null)
        {
            if (!ModLoader.TryGetMod(name1, out Mod mod))
                caller.Reply("Unknown Mod: " + name1, Color.Red);
            else
            {
                mc = list.SingleOrDefault(c => c.Mod == mod);
                if (mc is null)
                    caller.Reply("Mod: " + name1 + " does not have a " + name + " command.", Color.Red);
            }
        }
        else if (list.Count > 1)
        {
            caller.Reply("Multiple definitions of command /" + name + ". Try:", Color.Red);
            foreach (ModCommand modCommand in list)
                caller.Reply(modCommand.Mod.Name + ":" + modCommand.Command, Color.LawnGreen);
        }
        else mc = list[0];
        
        return true;
    }
}