using System;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreCommands.IL;
using MoreCommands.Utils;

namespace MoreCommands.Extensions;

public static class ILCursorExtensions
{
    public static void EmitMethodCall(this ILCursor c, Type owner, string methodName, bool isStatic = false, bool isPublic = false, Type[] types = null, params ILParameter[] parameters)
    {
        MethodInfo method = Util.GetMethod(owner, methodName, isStatic, isPublic, types);
        
        foreach (ILParameter param in parameters)
            param.Inject(c);
        
        c.Emit(method.IsVirtual || method.DeclaringType is not null && method.DeclaringType.IsInterface ? OpCodes.Callvirt : OpCodes.Call, method);
    }

    public static void EmitDelegate<T>(this ILCursor c, T @delegate, params ILParameter[] parameters) where T : Delegate
    {
        foreach (ILParameter param in parameters)
            param.Inject(c);

        c.EmitDelegate(@delegate);
    }

    public static void Emit(this ILCursor c, ILParameter param) => param.Inject(c);
}