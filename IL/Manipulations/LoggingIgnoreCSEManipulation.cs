using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Brigadier.NET.Exceptions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreCommands.Extensions;
using MoreCommands.Utils;
using Terraria.ModLoader;

namespace MoreCommands.IL.Manipulations;

public class LoggingIgnoreCSEManipulation : ILManipulation
{
    public override MethodBase Target => Util.GetMethod(typeof(Logging), "FirstChanceExceptionHandler", true, false);
    public override IEnumerable<ILMove> Movements => Array.Empty<ILMove>();
    
    public override void Inject(ILCursor c)
    {
        ILLabel label = c.DefineLabel();
        c.EmitDelegate((FirstChanceExceptionEventArgs args) => args.Exception is CommandSyntaxException, ILParameter.LoadArg(1)); // Push bool denoting if the exception is a CSE on the stack.
        c.Emit(OpCodes.Brfalse, label); // If it is not a CSE, continue with the rest of the code.
        c.Emit(OpCodes.Ret); // If it is a CSE, return.
        c.MarkLabel(label); // Mark the label onto the original code.
    }
}