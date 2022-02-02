using System.Collections.Generic;
using System.Reflection;
using MonoMod.Cil;
using MoreCommands.Utils;
using Terraria;
using Terraria.GameContent.UI.Chat;

namespace MoreCommands.IL.Manipulations;

public class SuggestionsScrollManipulation : ILManipulation
{
    public override MethodBase Target => Util.GetMethod(typeof(Main), "DoUpdate_HandleChat", isPublic:false);
    public override IEnumerable<ILMove> Movements => new[]
    {
        Move(MoveType.Before, inst => inst.MatchCallvirt(typeof(IChatMonitor).GetMethod("Offset")))
    };
    
    public override void Inject(ILCursor c)
    {
        c.Remove(); // Don't handle up and down keys for the chat monitor when the suggestions UI is being drawn.
        c.EmitDelegate(delegate(IChatMonitor chatMonitor, int offset) // Chatmonitor and offset are on the stack at this point.
        {
            if (ChatCommandSuggestionsManipulation.UI is null) chatMonitor.Offset(offset);
        });
    }
}