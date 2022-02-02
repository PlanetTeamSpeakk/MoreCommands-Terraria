using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreCommands.Utils;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace MoreCommands.IL.Manipulations;

public class RainbowEEListenerManipulation : ILManipulation
{
    public override MethodBase Target => Util.GetMethod(typeof(Main), "DoUpdate_HandleChat", true, false);
    public override IEnumerable<ILMove> Movements => new[]
    {
        Move(inst => inst.MatchLdsfld(typeof(Main).GetField("chatText", BindingFlags.Public | BindingFlags.Static)) &&
                            inst.Next.MatchCallvirt(typeof(string).GetMethod("get_Length", BindingFlags.Public | BindingFlags.Instance)) && inst.Next.Next.MatchLdcI4(0) &&
                            inst.Next.Next.Next.OpCode == OpCodes.Ble_S) 
    };
    
    public override void Inject(ILCursor c)
    {
        ILLabel noRainbowLabel = c.DefineLabel();
        
        c.Emit(OpCodes.Pop); // Jumps are made to this instruction, so instead of removing it, we pop the value it loads and load it again when we're done.
        c.EmitDelegate(() => Main.chatText.ToLower().StartsWith("/rainbow")); // Check if current chattext is '/rainbow'
        c.Emit(OpCodes.Brfalse, noRainbowLabel); // If not, continue with the method.
        c.EmitDelegate(() =>
        {
            Main.chatText = "";
            Main.ClosePlayerChat();
            Main.chatRelease = false;
            SoundEngine.PlaySound(SoundID.MenuClose);
            MoreCommands.Rainbow = !MoreCommands.Rainbow;
        }); // Toggle the rainbow.
        c.Emit(OpCodes.Ret); // Return out of the method.
        c.Emit(OpCodes.Ldsfld, typeof(Main).GetField("chatText", BindingFlags.Public | BindingFlags.Static)); // Load the chatText again.
        c.GotoPrev();
        c.MarkLabel(noRainbowLabel); // Move to previous ldsfld instruction if the text is not rainbows.
    }
}