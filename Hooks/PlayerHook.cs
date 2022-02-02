using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MoreCommands.Hooks;

public class PlayerHook : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (!MoreCommands.CommandKeybind.JustPressed) return;
        
        SoundEngine.PlaySound(10);
        Main.OpenPlayerChat();
        Main.chatText = "/";
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        if (Main.netMode != NetmodeID.Server || toWho != -1 || Netplay.Clients[fromWho].State != 10 || !MoreCommands.IsOp(fromWho)) return;
        // Player just joined and is op.
        ModPacket packet = MoreCommands.Instance.GetPacket();
        packet.Write((byte) 0);
        packet.Write(true);
        packet.Send(fromWho);
    }
}