using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace MoreCommands.IL;

public abstract class ILManipulation
{
    public abstract MethodBase Target { get; }
    public abstract IEnumerable<ILMove> Movements { get; }

    public abstract void Inject(ILCursor c);
    
    protected static ILMove Move(Func<Instruction, bool> predicate) => ILMove.Move(MoveType.After, predicate);
    
    protected static ILMove Move(MoveType type, Func<Instruction, bool> predicate) => ILMove.Move(type, predicate);

    protected static ILMove MovePrev(Func<Instruction, bool> predicate) => ILMove.MovePrev(MoveType.After, predicate);
    
    protected static ILMove MovePrev(MoveType type, Func<Instruction, bool> predicate) => ILMove.MovePrev(type, predicate);
}