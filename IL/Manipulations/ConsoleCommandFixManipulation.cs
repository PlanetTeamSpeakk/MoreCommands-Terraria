using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreCommands.Extensions;
using MoreCommands.IL.Detours;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;

namespace MoreCommands.IL.Manipulations;

// Fixes input from commands ran in the console being lowercase.
public class ConsoleCommandFixManipulation : ILManipulation
{
    public override MethodBase Target => Util.GetMethod(typeof(Main), "ExecuteCommand");

    public override IEnumerable<ILMove> Movements => new[]
    {
        Move(inst => inst.MatchCall(typeof(CommandLoader).GetMethod("HandleCommand", BindingFlags.NonPublic | BindingFlags.Static))),
        MovePrev(inst => inst.MatchLdarg(0)) 
    };

    public override void Inject(ILCursor c)
    {
        c.Emit(OpCodes.Pop); // Seeing as jumps are made to this instruction, the easiest way to get rid of it is by just popping the loaded value off the stack.
        c.Emit(OpCodes.Ldloc_0); // Load local variable 0 which is the non-lowered version of the input string
    }
}