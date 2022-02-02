using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MoreCommands.Misc;

public class ServerPlayerCommandCaller : CommandCaller
{
    public ServerPlayerCommandCaller(Player player) => Player = player;

    public CommandType CommandType => CommandType.Server;

    public Player Player { get; }

    public void Reply(string text, Color color = default)
    {
        if (color == new Color())
            color = Color.White;
        
        foreach (string text1 in text.Split('\n'))
            ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(text1), color, Player.whoAmI);
    }
}