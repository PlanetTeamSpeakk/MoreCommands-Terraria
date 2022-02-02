using System.IO;
using MoreCommands.Misc;
using Terraria;
using Terraria.Chat;
using Terraria.UI.Chat;

namespace MoreCommands.IL.Detours;

public class NetTextModuleDetours
{
    public static bool DeserializeAsServer(BinaryReader reader, int senderPlayerId)
    {
        ChatMessage message = ChatMessage.Deserialize(reader);
        if (CommandLoaderDetours.HandleCommand(message.Text, new ServerPlayerCommandCaller(Main.player[senderPlayerId])))
        {
            message.Consume();
            return true;
        }

        ChatManager.Commands.ProcessIncomingMessage(message, senderPlayerId);
        return true;
    }
}