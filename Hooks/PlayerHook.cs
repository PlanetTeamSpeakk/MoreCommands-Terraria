using MoreCommands.Misc;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

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
        packet.Write((byte) MCPacketID.S2C.OperatorPacket);
        packet.Write(true);
        packet.Send(fromWho);
    }

    public override bool ShiftClickSlot(Item[] inventory, int context, int slot) => 
        MoreCommands.DisposalInterface.CurrentState is not null && MoreCommands.DisposalUI.OnShiftClick(inventory, slot);
}