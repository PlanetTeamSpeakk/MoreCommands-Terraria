using System;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace MoreCommands.Misc;

public class DummyCommandCaller : CommandCaller
{
    public CommandType CommandType => CommandType.Console;

    public Terraria.Player Player => null;

    public void Reply(string text, Color color = default)
    {
        foreach (string str in text.Split('\n'))
            Console.WriteLine(str);
    }
}