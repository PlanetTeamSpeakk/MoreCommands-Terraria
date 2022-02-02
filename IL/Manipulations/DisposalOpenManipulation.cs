using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreCommands.Commands.Client;
using MoreCommands.Extensions;
using MoreCommands.Utils;
using Terraria;

namespace MoreCommands.IL.Manipulations;

public class DisposalOpenManipulation : ILManipulation
{
    public override MethodBase Target => Util.GetMethod(typeof(Player), "HandleBeingInChestRange", false, false);
    public override IEnumerable<ILMove> Movements => new[]
    {
        Move(inst => inst.MatchLdcI4(-1) && inst.Previous.MatchLdfld(typeof(Player).GetField("chest", BindingFlags.Public | BindingFlags.Instance))), 
    };
    
    public override void Inject(ILCursor c)
    {
        c.Emit(OpCodes.Ceq); // Check if Player.chest is equal to -1 and push it onto the stack.
        c.EmitDelegate((Player player) => player.chest == DisposalCommand.Disposal, ILParameter.LoadArg(0));
        c.Emit(OpCodes.Or); // Check if either Player.chest is equal to -1 or to Disposal
        c.Instrs[c.Index].OpCode = OpCodes.Brtrue; // Jump to the else statement if previous instruction is true.
    }
}