using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.Misc;

public class ClientPlayerCommandCaller : CommandCaller
{
    public CommandType CommandType => CommandType.Chat;

    public Player Player => Main.player[Main.myPlayer];

    public void Reply(string text, Color color = default)
    {
        if (color == new Color())
            color = Color.White;
        
        foreach (string text1 in text.Split('\n'))
            Main.NewText(text1, color.R, color.G, color.B);
    }
}