using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace MoreCommands.IL;

public class ILMove
{
    private readonly MoveType _type;
    private readonly Func<Instruction, bool> _predicate;
    private readonly bool _movePrev;
    private ILMove(MoveType type, Func<Instruction, bool> predicate, bool movePrev = false)
    {
        _type = type;
        _predicate = predicate;
        _movePrev = movePrev;
    }

    public static ILMove Move(Func<Instruction, bool> predicate) => new(MoveType.After, predicate);
    
    public static ILMove Move(MoveType type, Func<Instruction, bool> predicate) => new(type, predicate);

    public static ILMove MovePrev(Func<Instruction, bool> predicate) => new(MoveType.After, predicate, true);
    
    public static ILMove MovePrev(MoveType type, Func<Instruction, bool> predicate) => new(type, predicate, true);

    internal bool Move(ILCursor c) => _movePrev ? c.TryGotoPrev(_type, _predicate) : c.TryGotoNext(_type, _predicate);
}