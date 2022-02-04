using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MoreCommands.Misc;

public class CommandSource
{
    public CommandCaller Caller { get; }
    public bool IsPlayer => Caller.Player is not null;
    public Player Player => IsPlayer ? Caller.Player : throw MCBuiltInExceptions.BePlayer.Create();
    public Vector2 Pos { get; }
    public bool IsOp { get; }
    public bool IsSilent { get; }

    public CommandSource(CommandCaller caller) : this(caller, caller.Player?.TopLeft ?? Vector2.Zero,
        // Either the player is on singleplayer and has cheats enabled, the player is an operator on the server or the server told the client they're an operator.
        Main.netMode == NetmodeID.SinglePlayer && ClientConfig.Instance.EnableCheats || 
        Main.netMode != NetmodeID.SinglePlayer && (caller.Player is null || 
                                                   Main.netMode == NetmodeID.Server && MoreCommands.IsOp(caller.Player.whoAmI) || 
                                                   MoreCommands.IsClientOp), false) {}

    private CommandSource(CommandCaller caller, Vector2 pos, bool op, bool silent)
    {
        Caller = caller;
        Pos = pos;
        IsOp = op;
        IsSilent = silent;
    }

    public CommandSource WithCaller(CommandCaller caller) => new(caller, Pos, IsOp, IsSilent);
    
    public CommandSource WithPosition(Vector2 pos) => new(Caller, pos, IsOp, IsSilent);
    
    public CommandSource WithPosition((float x, float y) pos) => new(Caller, new Vector2(pos.x, pos.y), IsOp, IsSilent);

    public CommandSource WithOp(bool op) => new(Caller, Pos, op, IsSilent);

    public CommandSource WithSilent(bool silent) => new(Caller, Pos, IsOp, silent);

    public void Reply(string message) => Reply(message, MoreCommands.DefColour);

    public void Reply(string message, Color color)
    {
        if (!IsSilent) Caller.Reply(message, color);
    }

    public void Error(string message) => Reply(message, Color.Red);
    
    public void Severe(string message) => Reply(message, Color.DarkRed);

    public static bool operator ==(CommandSource first, CommandSource second) => first?.Equals(second) ?? second is null;
    
    public static bool operator !=(CommandSource first, CommandSource second) => !(first == second);

    private bool Equals(CommandSource other)
    {
        return Equals(Caller, other.Caller) && Pos.Equals(other.Pos) && IsOp == other.IsOp && IsSilent == other.IsSilent;
    }
    
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((CommandSource) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Caller, Pos, IsOp, IsSilent);
    }

    public override string ToString()
    {
        return $"CommandSource{{{nameof(Caller)}: {Caller}, {nameof(IsPlayer)}: {IsPlayer}, {nameof(Player)}: {Player}, {nameof(Pos)}: {Pos}, {nameof(IsOp)}: {IsOp}, {nameof(IsSilent)}: {IsSilent}}}";
    }
}